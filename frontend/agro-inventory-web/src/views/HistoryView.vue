<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useToast } from 'primevue/usetoast'
import { historyApi, MovementType } from '../api/history'
import type { HistoryItemDto, HistoryDetailDto, EditMovementRequest, HistoryFilters } from '../api/history'
import { useReference } from '../composables/useReference'
import { exportApi } from '../api/export'
import { localToIso } from '../utils/datetime'
import { ApiError } from '../api/http'
import { unitLabel } from '../api/types'

const toast = useToast()
const ref_ = useReference()
const items = ref<HistoryItemDto[]>([])
const loading = ref(false)
const exporting = ref(false)

async function exportExcel() {
  exporting.value = true
  try {
    await exportApi.history(filters.value)
  } catch (e) {
    fail(e, 'Не удалось выгрузить Excel')
  } finally {
    exporting.value = false
  }
}

const filters = ref<HistoryFilters>({})
const typeOptions = [
  { label: 'Приход', value: MovementType.Income },
  { label: 'Списание', value: MovementType.Outcome },
  { label: 'Корректировка', value: MovementType.Correction },
  { label: 'Перемещение', value: MovementType.Transfer },
]

// Фильтры на мобилке прячем за кнопкой (как на странице «Химия»). На десктопе видны всегда.
const filtersVisible = ref(false)
const activeFiltersCount = computed(() =>
  [filters.value.chemicalId, filters.value.movementType, filters.value.warehouseId, filters.value.cropId, filters.value.fieldId]
    .filter((v) => v !== undefined && v !== null && v !== '').length,
)
const filterBadge = computed(() => (activeFiltersCount.value ? String(activeFiltersCount.value) : undefined))

const detail = ref<HistoryDetailDto | null>(null)
const detailDialog = ref(false)
const editDialog = ref(false)
const edit = ref({ occurredAtLocal: '', comment: '', quantity: 0, cropId: '' as string | null, fieldId: '' as string | null })
const detailUnit = computed(() => unitLabel(detail.value?.measureUnit))

// Пустой GUID = явная очистка поля при редактировании (backend отличает от «не менять»).
const EMPTY_GUID = '00000000-0000-0000-0000-000000000000'

let timer: ReturnType<typeof setTimeout> | undefined

async function load() {
  loading.value = true
  try {
    items.value = await historyApi.list(filters.value)
  } finally {
    loading.value = false
  }
}

watch(filters, () => { clearTimeout(timer); timer = setTimeout(load, 300) }, { deep: true })

function typeLabel(t?: number) {
  return t === MovementType.Income ? 'Приход' : t === MovementType.Outcome ? 'Списание' : t === MovementType.Transfer ? 'Перемещение' : 'Корректировка'
}
function typeSeverity(t?: number) {
  return t === MovementType.Income ? 'success' : t === MovementType.Outcome ? 'warn' : t === MovementType.Transfer ? 'secondary' : 'info'
}
function fmtDate(iso: string) { return new Date(iso).toLocaleString('ru-RU') }
function fail(e: unknown, fb: string) {
  toast.add({ severity: 'error', summary: 'Ошибка', detail: e instanceof ApiError ? e.message : fb, life: 4000 })
}

async function openDetail(item: HistoryItemDto) {
  try {
    detail.value = await historyApi.get(item.id!)
    detailDialog.value = true
  } catch (e) { fail(e, 'Не удалось загрузить операцию') }
}

function openEdit() {
  const d = detail.value!
  edit.value = {
    occurredAtLocal: toLocal(d.occurredAt!),
    comment: d.comment ?? '',
    quantity: d.quantity ?? 0,
    cropId: d.cropId ?? null,
    fieldId: d.fieldId ?? null,
  }
  editDialog.value = true
}

function toLocal(iso: string) {
  const d = new Date(iso)
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`
}

async function saveEdit() {
  const d = detail.value!
  const body: EditMovementRequest = {
    occurredAt: edit.value.occurredAtLocal ? localToIso(edit.value.occurredAtLocal) : null,
    comment: edit.value.comment,
    cropId: d.movementType === MovementType.Outcome ? edit.value.cropId : null,
    fieldId: d.movementType === MovementType.Outcome ? (edit.value.fieldId || EMPTY_GUID) : null,
    quantity: edit.value.quantity,
  }
  try {
    await historyApi.update(d.id!, body)
    editDialog.value = false
    detailDialog.value = false
    toast.add({ severity: 'success', summary: 'Операция изменена', life: 2000 })
    await load()
  } catch (e) { fail(e, 'Не удалось изменить операцию') }
}

async function remove(item: HistoryItemDto) {
  if (!window.confirm('Удалить операцию? Она скроется из истории, но останется в аудите.')) return
  try {
    await historyApi.remove(item.id!)
    detailDialog.value = false
    toast.add({ severity: 'success', summary: 'Операция удалена', life: 2000 })
    await load()
  } catch (e) { fail(e, 'Не удалось удалить операцию') }
}

onMounted(async () => { await ref_.load(); await load() })
</script>

<template>
  <section class="page">
    <div class="toolbar">
    <div class="head">
      <h1 class="page__title">История</h1>
      <div class="head__actions">
        <PvButton class="filter-toggle" icon="pi pi-filter" rounded
          :outlined="!filtersVisible" :severity="activeFiltersCount ? 'info' : undefined"
          :badge="filterBadge" aria-label="Фильтры" :aria-expanded="filtersVisible"
          aria-controls="history-filters" @click="filtersVisible = !filtersVisible" />
        <PvButton label="Excel" icon="pi pi-file-excel" outlined :loading="exporting" @click="exportExcel" />
      </div>
    </div>

    <div id="history-filters" class="filters" :class="{ 'filters--open': filtersVisible }">
      <PvSelect v-model="filters.chemicalId" :options="ref_.chemicalOptions.value" option-label="label" option-value="value" filter show-clear placeholder="Химия" />
      <PvSelect v-model="filters.movementType" :options="typeOptions" option-label="label" option-value="value" show-clear placeholder="Тип" />
      <PvSelect v-model="filters.warehouseId" :options="ref_.warehouseOptions.value" option-label="label" option-value="value" show-clear placeholder="Склад" />
      <PvSelect v-model="filters.cropId" :options="ref_.cropOptions.value" option-label="label" option-value="value" filter show-clear placeholder="Культура" />
      <PvSelect v-model="filters.fieldId" :options="ref_.fieldOptions.value" option-label="label" option-value="value" filter show-clear placeholder="Поле" />
    </div>
    </div>

    <!-- Десктоп: таблица -->
    <div class="desktop-table mt">
      <PvDataTable :value="items" :loading="loading" data-key="id">
        <PvColumn header="Дата"><template #body="{ data }">{{ fmtDate(data.occurredAt) }}</template></PvColumn>
        <PvColumn header="Тип"><template #body="{ data }">
          <PvTag :value="typeLabel(data.movementType)" :severity="typeSeverity(data.movementType)" />
          <PvTag v-if="data.fieldTreatmentId" value="Обработка поля" severity="info" class="ml" />
        </template></PvColumn>
        <PvColumn field="chemicalName" header="Химия" />
        <PvColumn header="Кол-во"><template #body="{ data }">{{ data.quantity }} {{ unitLabel(data.measureUnit) }}</template></PvColumn>
        <PvColumn header="Склад"><template #body="{ data }">
          <span v-if="data.movementType === MovementType.Transfer">Склад {{ data.warehouseNumber }} → {{ data.targetWarehouseNumber }}</span>
          <span v-else>Склад {{ data.warehouseNumber }}</span>
        </template></PvColumn>
        <PvColumn field="cropName" header="Культура" />
        <PvColumn header="Поле"><template #body="{ data }">{{ data.fieldNumber ?? '—' }}</template></PvColumn>
        <PvColumn header="" style="width: 5rem"><template #body="{ data }">
          <PvButton icon="pi pi-info-circle" text rounded @click="openDetail(data)" />
        </template></PvColumn>
        <template #empty><div class="empty">Операций нет</div></template>
      </PvDataTable>
    </div>

    <!-- Мобилка: карточки -->
    <div class="history-cards">
      <div v-if="loading" class="empty">Загрузка…</div>
      <template v-else>
        <article v-for="item in items" :key="item.id!" class="history-card" role="button" tabindex="0"
          @click="openDetail(item)" @keydown.enter="openDetail(item)" @keydown.space.prevent="openDetail(item)">
          <div class="history-card__top">
            <PvTag :value="typeLabel(item.movementType)" :severity="typeSeverity(item.movementType)" />
            <span class="history-card__date">{{ fmtDate(item.occurredAt!) }}</span>
          </div>
          <div class="history-card__name">{{ item.chemicalName }}</div>
          <div class="history-card__meta">
            <span class="history-card__qty">{{ item.quantity }} {{ unitLabel(item.measureUnit) }}</span>
            <span v-if="item.movementType === MovementType.Transfer">Склад {{ item.warehouseNumber }} → {{ item.targetWarehouseNumber }}</span>
            <span v-else>Склад {{ item.warehouseNumber }}</span>
            <span v-if="item.cropName">{{ item.cropName }}</span>
            <span v-if="item.fieldNumber">Поле {{ item.fieldNumber }}</span>
            <span v-if="item.fieldTreatmentId" class="history-card__link">Обработка поля</span>
          </div>
        </article>
        <div v-if="!items.length" class="empty">Операций нет</div>
      </template>
    </div>

    <!-- Детали операции -->
    <PvDialog v-model:visible="detailDialog" header="Операция" modal :style="{ width: '30rem' }">
      <div v-if="detail" class="detail">
        <div><b>{{ typeLabel(detail.movementType) }}</b> · {{ fmtDate(detail.occurredAt!) }}</div>
        <PvTag v-if="detail.fieldTreatmentId" value="Создано обработкой поля" severity="info" class="fit" />
        <div v-if="detail.movementType === MovementType.Transfer">
          {{ detail.chemicalName }} — {{ detail.quantity }} {{ detailUnit }} · Склад {{ detail.warehouseNumber }} → {{ detail.targetWarehouseNumber }}
        </div>
        <div v-else>{{ detail.chemicalName }} — {{ detail.quantity }} {{ detailUnit }} · Склад {{ detail.warehouseNumber }}</div>
        <div v-if="detail.cropName">Культура: {{ detail.cropName }}</div>
        <div v-if="detail.fieldNumber">Поле: {{ detail.fieldNumber }}</div>
        <div v-if="detail.comment">Комментарий: {{ detail.comment }}</div>
      </div>
      <template #footer>
        <PvButton v-if="detail?.movementType !== MovementType.Transfer" label="Удалить" text severity="danger" @click="remove(detail!)" />
        <PvButton v-if="detail?.movementType !== MovementType.Transfer" label="Редактировать" outlined @click="openEdit" />
        <PvButton label="Закрыть" text @click="detailDialog = false" />
      </template>
    </PvDialog>

    <!-- Редактирование операции -->
    <PvDialog v-model:visible="editDialog" header="Редактировать операцию" modal :style="{ width: '28rem' }">
      <div class="field"><span>Дата и время</span>
        <input class="dt" type="datetime-local" v-model="edit.occurredAtLocal" />
      </div>
      <div class="field"><span>Количество, {{ detailUnit }}</span><PvInputText v-model.number="edit.quantity" type="number" /></div>
      <div v-if="detail?.movementType === MovementType.Outcome" class="field"><span>Культура</span>
        <PvSelect v-model="edit.cropId" :options="ref_.cropOptions.value" option-label="label" option-value="value" filter />
      </div>
      <div v-if="detail?.movementType === MovementType.Outcome" class="field"><span>Номер поля</span>
        <PvSelect v-model="edit.fieldId" :options="ref_.fieldOptions.value" option-label="label" option-value="value" filter show-clear placeholder="Не указано" />
      </div>
      <div class="field"><span>Комментарий</span><PvTextarea v-model="edit.comment" rows="2" auto-resize /></div>
      <template #footer>
        <PvButton label="Отмена" text @click="editDialog = false" />
        <PvButton label="Сохранить" @click="saveEdit" />
      </template>
    </PvDialog>
  </section>
</template>

<style scoped>
.head { display: flex; justify-content: space-between; align-items: center; gap: 1rem; }
.head__actions { display: flex; gap: 0.5rem; align-items: center; }
.filters { display: flex; gap: 0.5rem; flex-wrap: wrap; }
.mt { margin-top: 1rem; }
.ml { margin-left: 0.35rem; }
.fit { width: fit-content; }
.empty { padding: 1rem; color: #6b7280; }
/* Кнопка-фильтр и карточки — только на мобилке. */
.filter-toggle { display: none; }
.history-cards { display: none; }

@media (max-width: 640px) {
  .filter-toggle { display: inline-flex; }
  /* Шапка + фильтры закреплены, список карточек скроллится под ними. */
  .toolbar {
    position: sticky;
    top: 0;
    z-index: 15;
    margin: -1rem -1rem 0;
    padding: 0.75rem 1rem;
    background: var(--p-content-background, #fff);
    border-bottom: 1px solid var(--p-content-border-color, #e5e7eb);
  }
  .filters { display: none; flex-direction: column; }
  .filters.filters--open { display: flex; }
  .filters :deep(.p-select) { width: 100%; }
  .desktop-table { display: none; }
  .history-cards { display: flex; flex-direction: column; gap: 0.75rem; margin-top: 1rem; }
  .history-card {
    display: flex; flex-direction: column; gap: 0.5rem;
    width: 100%; padding: 0.875rem; text-align: left;
    border: 1px solid #e5e7eb; border-radius: 10px; background: #fff;
    color: inherit; font: inherit; cursor: pointer;
  }
  .history-card__top { display: flex; align-items: center; justify-content: space-between; gap: 0.75rem; }
  .history-card__date { color: #6b7280; font-size: 0.85rem; }
  .history-card__name { font-weight: 700; color: #374151; }
  .history-card__meta { display: flex; flex-wrap: wrap; gap: 0.35rem 0.75rem; color: #4b5563; font-size: 0.9rem; }
  .history-card__qty { font-weight: 600; color: #111827; }
  .history-card__link { color: #15803d; font-weight: 600; }
}
.detail { display: flex; flex-direction: column; gap: 0.4rem; }
.sources ul { margin: 0.25rem 0; padding-left: 1.25rem; }
.field { display: flex; flex-direction: column; gap: 0.25rem; margin-bottom: 0.75rem; }
.field > span { font-weight: 600; font-size: 0.9rem; }
.dt { padding: 0.5rem; border: 1px solid #d1d5db; border-radius: 6px; font: inherit; }
/* iOS Safari: нативный datetime-local распирает экран — сдавливаем до контейнера на мобилке */
@media (max-width: 640px) {
  .dt { width: 100%; min-width: 0; max-width: 100%; box-sizing: border-box; -webkit-appearance: none; appearance: none; }
}
</style>
