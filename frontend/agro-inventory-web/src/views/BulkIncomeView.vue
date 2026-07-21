<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRouter, onBeforeRouteLeave } from 'vue-router'
import { useToast } from 'primevue/usetoast'
import { canonicalApi } from '../api/catalog'
import { inventoryApi } from '../api/inventory'
import type { BulkIncomeCompanyOptionDto, BulkIncomeLineRequest } from '../api/inventory'
import type { CanonicalChemicalDto } from '../api/types'
import { ApiError } from '../api/http'
import { nowLocalInput, localToIso } from '../utils/datetime'

const router = useRouter()
const toast = useToast()

const canonicalChemicals = ref<CanonicalChemicalDto[]>([])
const canonicalId = ref<string | null>(null)
const occurredAt = ref(nowLocalInput())
const comment = ref('')
const companies = ref<BulkIncomeCompanyOptionDto[]>([])
const selected = ref<Record<string, boolean>>({})
const warehouses = ref<Record<string, string | null>>({})
const quantities = ref<Record<string, number | null>>({})
const loading = ref(false)
const saving = ref(false)
const done = ref(false)
const dirty = ref(false)

const canonicalOptions = computed(() =>
  canonicalChemicals.value.map((c) => ({ label: c.canonicalName, value: c.id })),
)
const availableCompanies = computed(() =>
  companies.value.filter((c) => c.companyId && !c.blockedReason),
)
const lines = computed<BulkIncomeLineRequest[]>(() =>
  availableCompanies.value
    .filter((c) => c.companyId && selected.value[c.companyId])
    .map((c) => ({
      companyId: c.companyId!,
      warehouseId: warehouses.value[c.companyId!]!,
      quantity: quantities.value[c.companyId!] ?? 0,
    }))
    .filter((l) => l.warehouseId && l.quantity > 0),
)

function markDirty() { dirty.value = true }
function fail(e: unknown, fallback: string) {
  toast.add({ severity: 'error', summary: 'Ошибка', detail: e instanceof ApiError ? e.message : fallback, life: 5000 })
}
function warehouseOptions(company: BulkIncomeCompanyOptionDto) {
  return (company.warehouses ?? []).map((w) => ({ label: `Склад ${w.warehouseNumber}`, value: w.warehouseId }))
}

async function loadOptions() {
  companies.value = []
  selected.value = {}
  warehouses.value = {}
  quantities.value = {}
  if (!canonicalId.value) return

  loading.value = true
  try {
    const result = await inventoryApi.bulkIncomeOptions(canonicalId.value)
    companies.value = result.companies ?? []
    for (const company of companies.value) {
      if (!company.companyId || company.blockedReason) continue
      selected.value[company.companyId] = true
      warehouses.value[company.companyId] = company.warehouses?.[0]?.warehouseId ?? null
      quantities.value[company.companyId] = null
    }
  } catch (e) {
    fail(e, 'Не удалось загрузить хозяйства для прихода')
  } finally {
    loading.value = false
  }
}

function valid(): boolean {
  if (!canonicalId.value) return false
  return lines.value.length > 0
}

async function submit() {
  if (!valid()) {
    toast.add({ severity: 'warn', summary: 'Заполните строки прихода', life: 3000 })
    return
  }

  saving.value = true
  try {
    await inventoryApi.bulkIncome({
      canonicalChemicalId: canonicalId.value!,
      occurredAt: localToIso(occurredAt.value),
      comment: comment.value.trim() || null,
      lines: lines.value,
    })
    dirty.value = false
    done.value = true
    toast.add({ severity: 'success', summary: 'Приходы добавлены', life: 2500 })
  } catch (e) {
    fail(e, 'Не удалось добавить приходы')
  } finally {
    saving.value = false
  }
}

function close() { router.back() }

watch(canonicalId, () => {
  markDirty()
  loadOptions()
})

onBeforeRouteLeave(() => (dirty.value && !done.value)
  ? window.confirm('У вас есть несохранённые изменения. Выйти без сохранения?') : true)

onMounted(async () => {
  canonicalChemicals.value = await canonicalApi.list()
})
</script>

<template>
  <section class="page form">
    <div class="head">
      <h1 class="page__title">Приход в несколько хозяйств</h1>
      <PvButton label="Обычный приход" icon="pi pi-arrow-right" outlined @click="router.push({ name: 'income' })" />
    </div>

    <PvMessage v-if="done" severity="success" :closable="false" class="mb">Приходы добавлены</PvMessage>

    <template v-if="!done">
      <label class="field"><span>Общий препарат *</span>
        <PvSelect
          v-model="canonicalId"
          :options="canonicalOptions"
          option-label="label"
          option-value="value"
          filter
          placeholder="Выберите препарат"
        />
      </label>

      <label class="field"><span>Дата и время</span>
        <input class="dt" type="datetime-local" v-model="occurredAt" @change="markDirty" />
      </label>

      <label class="field"><span>Комментарий</span>
        <PvTextarea v-model="comment" rows="2" auto-resize @input="markDirty" />
      </label>

      <div class="companies">
        <div class="companies__head">
          <h2>Хозяйства</h2>
          <span v-if="lines.length">Будет создано: {{ lines.length }}</span>
        </div>

        <div v-if="loading" class="empty">Загрузка...</div>
        <div v-else-if="!canonicalId" class="empty">Выберите общий препарат, чтобы увидеть хозяйства.</div>
        <div v-else-if="!companies.length" class="empty">Нет доступных хозяйств.</div>

        <div
          v-for="company in companies"
          v-else
          :key="company.companyId"
          class="company-row"
          :class="{ 'company-row--blocked': company.blockedReason }"
        >
          <label class="company-row__check">
            <input
              v-if="!company.blockedReason"
              v-model="selected[company.companyId!]"
              type="checkbox"
              @change="markDirty"
            />
            <span v-else class="company-row__dash">—</span>
          </label>

          <div class="company-row__main">
            <div class="company-row__title">{{ company.companyName }}</div>
            <div v-if="company.chemicalName" class="company-row__chem">{{ company.chemicalName }}</div>
            <div v-if="company.blockedReason" class="company-row__blocked">{{ company.blockedReason }}</div>
          </div>

          <PvSelect
            v-if="!company.blockedReason"
            v-model="warehouses[company.companyId!]"
            :options="warehouseOptions(company)"
            option-label="label"
            option-value="value"
            placeholder="Склад"
            class="company-row__warehouse"
            @change="markDirty"
          />
          <PvInputText
            v-if="!company.blockedReason"
            v-model.number="quantities[company.companyId!]"
            type="number"
            min="0"
            placeholder="кол-во"
            class="company-row__qty"
            @input="markDirty"
          />
        </div>
      </div>

      <div class="row">
        <PvButton label="Создать приходы" icon="pi pi-check" :loading="saving" :disabled="!valid()" @click="submit" />
        <PvButton label="Отмена" text @click="close" />
      </div>
    </template>

    <div v-else class="row">
      <PvButton label="Закрыть" @click="close" />
      <PvButton label="Создать ещё" outlined @click="done = false" />
    </div>
  </section>
</template>

<style scoped>
.form { max-width: 920px; }
.head { display: flex; justify-content: space-between; align-items: center; gap: 1rem; margin-bottom: 1rem; }
.field { display: flex; flex-direction: column; gap: 0.25rem; margin-bottom: 1rem; }
.field > span { font-weight: 600; font-size: 0.9rem; }
.grid2 { display: grid; grid-template-columns: 1fr 1fr; gap: 0.75rem; }
.dt { padding: 0.5rem; border: 1px solid var(--p-inputtext-border-color, #d1d5db); border-radius: 6px; font: inherit; }
.mb { margin-bottom: 1rem; }
.empty { padding: 1rem; color: #6b7280; }
.companies {
  overflow: hidden;
  margin-bottom: 1rem;
  border: 1px solid #e5e7eb;
  border-radius: 8px;
  background: #fff;
}
.companies__head {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 1rem;
  padding: 1rem;
  border-bottom: 1px solid #e5e7eb;
}
.companies__head h2 { margin: 0; font-size: 1.1rem; }
.companies__head span { color: #374151; font-weight: 600; }
.company-row {
  display: grid;
  grid-template-columns: auto minmax(0, 1fr) minmax(10rem, 13rem) 8rem;
  gap: 0.75rem;
  align-items: center;
  padding: 0.85rem 1rem;
  border-bottom: 1px solid #eef2f7;
}
.company-row:last-child { border-bottom: 0; }
.company-row--blocked { background: #f9fafb; }
.company-row__check { display: grid; place-items: center; width: 1.5rem; color: #9ca3af; }
.company-row__title { font-weight: 700; color: #374151; }
.company-row__chem { margin-top: 0.15rem; color: #6b7280; font-size: 0.9rem; }
.company-row__blocked { margin-top: 0.15rem; color: #b91c1c; font-size: 0.9rem; }
.company-row__qty { width: 100%; }
.row { display: flex; gap: 0.5rem; align-items: center; }

@media (max-width: 640px) {
  .head { align-items: flex-start; flex-direction: column; }
  .grid2 { grid-template-columns: 1fr; }
  .company-row {
    grid-template-columns: auto minmax(0, 1fr);
    align-items: start;
  }
  .company-row__warehouse,
  .company-row__qty {
    grid-column: 2;
    width: 100%;
    min-width: 0;
  }
  .dt { width: 100%; min-width: 0; max-width: 100%; box-sizing: border-box; -webkit-appearance: none; appearance: none; }
}
</style>
