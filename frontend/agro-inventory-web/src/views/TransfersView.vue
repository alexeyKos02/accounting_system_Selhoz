<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useToast } from 'primevue/usetoast'
import { inventoryApi } from '../api/inventory'
import type { TransferItemDto } from '../api/inventory'
import { useReference } from '../composables/useReference'
import { unitLabel } from '../api/types'
import { localToIso, nowLocalInput } from '../utils/datetime'
import { ApiError } from '../api/http'

const toast = useToast()
const ref_ = useReference()
const items = ref<TransferItemDto[]>([])
const loading = ref(false)
const saving = ref(false)

const chemicalId = ref<string | null>(null)
const sourceWarehouseId = ref<string | null>(null)
const targetWarehouseId = ref<string | null>(null)
const quantity = ref<number | null>(null)
const occurredAt = ref(nowLocalInput())
const comment = ref('')

const unit = computed(() => unitLabel(ref_.chemicalUnit(chemicalId.value)))

function fmtNum(n?: number | null) {
  return (n ?? 0).toLocaleString('ru-RU', { maximumFractionDigits: 3 })
}
function rowUnit(id?: string) {
  return unitLabel(ref_.chemicalUnit(id))
}

function fmtDate(iso?: string) {
  return iso ? new Date(iso).toLocaleString('ru-RU') : '—'
}

function fail(e: unknown, fallback: string) {
  toast.add({ severity: 'error', summary: 'Ошибка', detail: e instanceof ApiError ? e.message : fallback, life: 5000 })
}

async function load() {
  loading.value = true
  try {
    items.value = await inventoryApi.transfers()
  } catch (e) {
    fail(e, 'Не удалось загрузить перемещения')
  } finally {
    loading.value = false
  }
}

function valid() {
  return !!chemicalId.value
    && !!sourceWarehouseId.value
    && !!targetWarehouseId.value
    && sourceWarehouseId.value !== targetWarehouseId.value
    && !!quantity.value
    && quantity.value > 0
}

async function submit() {
  if (!valid()) {
    toast.add({ severity: 'warn', summary: 'Заполните обязательные поля', life: 3000 })
    return
  }
  saving.value = true
  try {
    await inventoryApi.transfer({
      chemicalId: chemicalId.value!,
      sourceWarehouseId: sourceWarehouseId.value!,
      targetWarehouseId: targetWarehouseId.value!,
      quantity: quantity.value!,
      occurredAt: localToIso(occurredAt.value),
      comment: comment.value.trim() || null,
    })
    quantity.value = null
    comment.value = ''
    toast.add({ severity: 'success', summary: 'Перемещение сохранено', life: 2500 })
    await load()
  } catch (e) {
    fail(e, 'Не удалось сохранить перемещение')
  } finally {
    saving.value = false
  }
}

onMounted(async () => {
  await ref_.load()
  await load()
})
</script>

<template>
  <section class="page">
    <h1 class="page__title">Перемещения</h1>

    <div class="form-card">
      <div class="grid">
        <label class="field"><span>Химия *</span>
          <PvSelect v-model="chemicalId" :options="ref_.chemicalOptions.value" option-label="label" option-value="value" filter placeholder="Выберите химию" />
        </label>
        <label class="field"><span>Откуда *</span>
          <PvSelect v-model="sourceWarehouseId" :options="ref_.warehouseOptions.value" option-label="label" option-value="value" filter placeholder="Склад отправитель" />
        </label>
        <label class="field"><span>Куда *</span>
          <PvSelect v-model="targetWarehouseId" :options="ref_.warehouseOptions.value" option-label="label" option-value="value" filter placeholder="Склад получатель" />
        </label>
        <label class="field"><span>Количество, {{ unit }} *</span>
          <PvInputText v-model.number="quantity" type="number" min="0" />
        </label>
      </div>

      <label class="field"><span>Дата и время</span>
        <input v-model="occurredAt" class="dt" type="datetime-local" />
      </label>
      <label class="field"><span>Комментарий</span><PvTextarea v-model="comment" rows="2" auto-resize /></label>
      <PvButton label="Переместить" icon="pi pi-arrow-right-arrow-left" :loading="saving" :disabled="!valid()" @click="submit" />
    </div>

    <div class="desktop-table mt">
      <PvDataTable :value="items" :loading="loading" data-key="id">
        <PvColumn header="Дата"><template #body="{ data }">{{ fmtDate(data.occurredAt) }}</template></PvColumn>
        <PvColumn field="chemicalName" header="Химия" />
        <PvColumn header="Маршрут"><template #body="{ data }">Склад {{ data.sourceWarehouseNumber }} → склад {{ data.targetWarehouseNumber }}</template></PvColumn>
        <PvColumn header="Количество"><template #body="{ data }">{{ fmtNum(data.quantity) }} {{ rowUnit(data.chemicalId) }}</template></PvColumn>
        <PvColumn field="comment" header="Комментарий" />
        <template #empty><div class="empty">Перемещений пока нет</div></template>
      </PvDataTable>
    </div>

    <div class="cards">
      <div v-if="loading" class="empty">Загрузка…</div>
      <template v-else>
        <article v-for="item in items" :key="item.id!" class="card">
          <div class="card__top">
            <strong>{{ item.chemicalName }}</strong>
            <span>{{ fmtDate(item.occurredAt) }}</span>
          </div>
          <div class="card__route">Склад {{ item.sourceWarehouseNumber }} → склад {{ item.targetWarehouseNumber }}</div>
          <div class="card__meta">{{ fmtNum(item.quantity) }} {{ rowUnit(item.chemicalId) }}</div>
          <div v-if="item.comment" class="card__meta">{{ item.comment }}</div>
        </article>
        <div v-if="!items.length" class="empty">Перемещений пока нет</div>
      </template>
    </div>
  </section>
</template>

<style scoped>
.form-card {
  display: flex; flex-direction: column; gap: 0.75rem;
  padding: 1rem; border: 1px solid #e5e7eb; border-radius: 8px; background: #fff;
}
.grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 0.75rem; }
.field { display: flex; flex-direction: column; gap: 0.25rem; }
.field > span { font-weight: 600; font-size: 0.9rem; }
.dt { padding: 0.5rem; border: 1px solid var(--p-inputtext-border-color, #d1d5db); border-radius: 6px; font: inherit; }
.confirm { display: flex; align-items: center; gap: 0.5rem; color: #374151; }
.mt { margin-top: 1rem; }
.empty { padding: 1rem; color: #6b7280; }
.cards { display: none; }
@media (max-width: 640px) {
  .grid { grid-template-columns: 1fr; }
  .dt { width: 100%; min-width: 0; max-width: 100%; box-sizing: border-box; -webkit-appearance: none; appearance: none; }
  .desktop-table { display: none; }
  .cards { display: flex; flex-direction: column; gap: 0.75rem; margin-top: 1rem; }
  .card { display: flex; flex-direction: column; gap: 0.35rem; padding: 0.875rem; border: 1px solid #e5e7eb; border-radius: 10px; background: #fff; }
  .card__top { display: flex; justify-content: space-between; gap: 0.75rem; color: #374151; }
  .card__top span, .card__meta { color: #6b7280; font-size: 0.9rem; }
  .card__route { font-weight: 700; color: #111827; }
}
</style>
