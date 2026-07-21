<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute } from 'vue-router'
import { useToast } from 'primevue/usetoast'
import { treatmentsApi } from '../api/treatments'
import type { FieldTreatmentDto } from '../api/treatments'
import { ApiError } from '../api/http'
import { useReference } from '../composables/useReference'
import { unitLabel } from '../api/types'
import { localToIso, nowLocalInput } from '../utils/datetime'

const toast = useToast()
const route = useRoute()
const ref_ = useReference()
const items = ref<FieldTreatmentDto[]>([])
const loading = ref(false)
const saving = ref(false)

const fieldId = ref<string | null>(null)
const chemicalId = ref<string | null>(null)
const warehouseId = ref<string | null>(null)
const cropId = ref<string | null>(null)
const mode = ref<'total' | 'rate'>('total')
const quantity = ref<number | null>(null)
const rate = ref<number | null>(null)
const treatedAt = ref(nowLocalInput())
const comment = ref('')

const unit = computed(() => unitLabel(ref_.chemicalUnit(chemicalId.value)))
const selectedField = computed(() =>
  ref_.fields.value.find((f) => f.id === fieldId.value) ?? null,
)
const calculatedQuantity = computed(() => {
  if (mode.value === 'total') return quantity.value
  const area = selectedField.value?.areaHectares
  return area && rate.value ? area * rate.value : null
})

function fmtNum(n?: number | null) {
  return (n ?? 0).toLocaleString('ru-RU', { maximumFractionDigits: 3 })
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
    items.value = await treatmentsApi.list()
  } catch (e) {
    fail(e, 'Не удалось загрузить обработки')
  } finally {
    loading.value = false
  }
}

function valid() {
  if (!fieldId.value || !chemicalId.value || !warehouseId.value || !cropId.value) return false
  if (mode.value === 'rate') return !!rate.value && rate.value > 0 && !!selectedField.value?.areaHectares
  return !!quantity.value && quantity.value > 0
}

async function submit() {
  if (!valid()) {
    toast.add({ severity: 'warn', summary: 'Заполните обязательные поля', life: 3000 })
    return
  }
  saving.value = true
  try {
    await treatmentsApi.create({
      fieldId: fieldId.value!,
      chemicalId: chemicalId.value!,
      warehouseId: warehouseId.value!,
      cropId: cropId.value!,
      quantity: mode.value === 'total' ? quantity.value : null,
      ratePerHectare: mode.value === 'rate' ? rate.value : null,
      treatedAt: localToIso(treatedAt.value),
      comment: comment.value.trim() || null,
    })
    quantity.value = null
    rate.value = null
    comment.value = ''
    toast.add({ severity: 'success', summary: 'Обработка сохранена', life: 2500 })
    await load()
  } catch (e) {
    fail(e, 'Не удалось сохранить обработку')
  } finally {
    saving.value = false
  }
}

onMounted(async () => {
  await ref_.load()
  if (typeof route.query.fieldId === 'string') fieldId.value = route.query.fieldId
  await load()
})
</script>

<template>
  <section class="page">
    <h1 class="page__title">Обработки полей</h1>

    <div class="form-card">
      <div class="grid">
        <label class="field"><span>Поле *</span>
          <PvSelect v-model="fieldId" :options="ref_.fieldOptions.value" option-label="label" option-value="value" filter placeholder="Выберите поле" />
        </label>
        <label class="field"><span>Культура *</span>
          <PvSelect v-model="cropId" :options="ref_.cropOptions.value" option-label="label" option-value="value" filter placeholder="Культура" />
        </label>
        <label class="field"><span>Химия *</span>
          <PvSelect v-model="chemicalId" :options="ref_.chemicalOptions.value" option-label="label" option-value="value" filter placeholder="Химия" />
        </label>
        <label class="field"><span>Склад *</span>
          <PvSelect v-model="warehouseId" :options="ref_.warehouseOptions.value" option-label="label" option-value="value" filter placeholder="Склад" />
        </label>
      </div>

      <div class="mode">
        <PvButton label="Всего" size="small" :outlined="mode !== 'total'" @click="mode = 'total'" />
        <PvButton :label="`Норма, ${unit}/га`" size="small" :outlined="mode !== 'rate'" @click="mode = 'rate'" />
      </div>

      <div class="grid">
        <label v-if="mode === 'total'" class="field"><span>Количество, {{ unit }} *</span>
          <PvInputText v-model.number="quantity" type="number" min="0" />
        </label>
        <label v-else class="field"><span>Норма, {{ unit }}/га *</span>
          <PvInputText v-model.number="rate" type="number" min="0" />
        </label>
        <label class="field"><span>Дата и время</span>
          <input v-model="treatedAt" class="dt" type="datetime-local" />
        </label>
      </div>

      <PvMessage v-if="mode === 'rate'" severity="info" :closable="false">
        {{ selectedField?.areaHectares ? `Будет списано ${fmtNum(calculatedQuantity)} ${unit} (${fmtNum(selectedField.areaHectares)} га × ${fmtNum(rate)} ${unit}/га)` : 'У выбранного поля не указана площадь.' }}
      </PvMessage>

      <label class="field"><span>Комментарий</span><PvTextarea v-model="comment" rows="2" auto-resize /></label>

      <PvButton label="Сохранить обработку" icon="pi pi-check" :loading="saving" :disabled="!valid()" @click="submit" />
    </div>

    <div class="desktop-table mt">
      <PvDataTable :value="items" :loading="loading" data-key="id">
        <PvColumn header="Дата"><template #body="{ data }">{{ fmtDate(data.treatedAt) }}</template></PvColumn>
        <PvColumn header="Поле"><template #body="{ data }">Поле {{ data.fieldNumber }}</template></PvColumn>
        <PvColumn field="cropName" header="Культура" />
        <PvColumn field="chemicalName" header="Химия" />
        <PvColumn header="Количество"><template #body="{ data }">{{ fmtNum(data.quantity) }} {{ unitLabel(data.measureUnit) }}</template></PvColumn>
        <PvColumn header="Норма"><template #body="{ data }">{{ data.ratePerHectare ? `${fmtNum(data.ratePerHectare)} ${unitLabel(data.measureUnit)}/га` : '—' }}</template></PvColumn>
        <PvColumn header="Склад"><template #body="{ data }">Склад {{ data.warehouseNumber }}</template></PvColumn>
        <template #empty><div class="empty">Обработок пока нет</div></template>
      </PvDataTable>
    </div>

    <div class="cards">
      <div v-if="loading" class="empty">Загрузка…</div>
      <template v-else>
        <article v-for="item in items" :key="item.id!" class="card">
          <div class="card__top">
            <strong>Поле {{ item.fieldNumber }}</strong>
            <span>{{ fmtDate(item.treatedAt) }}</span>
          </div>
          <div class="card__name">{{ item.chemicalName }}</div>
          <div class="card__meta">{{ item.cropName }} · {{ fmtNum(item.quantity) }} {{ unitLabel(item.measureUnit) }} · Склад {{ item.warehouseNumber }}</div>
          <div v-if="item.ratePerHectare" class="card__meta">Норма {{ fmtNum(item.ratePerHectare) }} {{ unitLabel(item.measureUnit) }}/га</div>
        </article>
        <div v-if="!items.length" class="empty">Обработок пока нет</div>
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
.mode { display: flex; gap: 0.5rem; flex-wrap: wrap; }
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
  .card__name { font-weight: 700; color: #111827; }
}
</style>
