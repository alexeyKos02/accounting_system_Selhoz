<script setup lang="ts">
import { onMounted, ref, watch } from 'vue'
import { useToast } from 'primevue/usetoast'
import { historyApi, MovementType } from '../api/history'
import type { HistoryItemDto, HistoryDetailDto, EditMovementRequest, HistoryFilters } from '../api/history'
import { useReference } from '../composables/useReference'
import { exportApi } from '../api/export'
import { localToIso } from '../utils/datetime'
import { ApiError } from '../api/http'
import { UnitType } from '../api/types'

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
]

const detail = ref<HistoryDetailDto | null>(null)
const detailDialog = ref(false)
const editDialog = ref(false)
const edit = ref({ occurredAtLocal: '', comment: '', quantityLiters: 0, packagesQuantity: 0, packageVolume: 0, cropId: '' as string | null })

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
  return t === MovementType.Income ? 'Приход' : t === MovementType.Outcome ? 'Списание' : 'Корректировка'
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

function sourceLabel(t?: number) {
  return t === 1 ? 'Наливом' : t === 2 ? 'Полная упаковка' : 'Вскрытая упаковка'
}

const isPackageIncome = () =>
  detail.value?.movementType === MovementType.Income && detail.value?.unitType !== UnitType.Liter

function openEdit() {
  const d = detail.value!
  edit.value = {
    occurredAtLocal: toLocal(d.occurredAt!),
    comment: d.comment ?? '',
    quantityLiters: d.quantityLiters ?? 0,
    packagesQuantity: d.packagesQuantity ?? 0,
    packageVolume: d.packageVolumeLiters ?? 0,
    cropId: d.cropId ?? null,
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
    quantityLiters: null, unit: undefined, packageVolumeLiters: null, packagesQuantity: null,
  }
  if (isPackageIncome()) {
    body.packagesQuantity = edit.value.packagesQuantity
    body.packageVolumeLiters = edit.value.packageVolume
  } else {
    body.quantityLiters = edit.value.quantityLiters
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
    <div class="head">
      <h1 class="page__title">История</h1>
      <PvButton label="Excel" icon="pi pi-file-excel" outlined :loading="exporting" @click="exportExcel" />
    </div>

    <div class="filters">
      <PvSelect v-model="filters.chemicalId" :options="ref_.chemicalOptions.value" option-label="label" option-value="value" filter show-clear placeholder="Химия" />
      <PvSelect v-model="filters.movementType" :options="typeOptions" option-label="label" option-value="value" show-clear placeholder="Тип" />
      <PvSelect v-model="filters.warehouseId" :options="ref_.warehouseOptions.value" option-label="label" option-value="value" show-clear placeholder="Склад" />
      <PvSelect v-model="filters.cropId" :options="ref_.cropOptions.value" option-label="label" option-value="value" filter show-clear placeholder="Культура" />
    </div>

    <PvDataTable :value="items" :loading="loading" data-key="id" class="mt">
      <PvColumn header="Дата"><template #body="{ data }">{{ fmtDate(data.occurredAt) }}</template></PvColumn>
      <PvColumn header="Тип"><template #body="{ data }">
        <PvTag :value="typeLabel(data.movementType)"
          :severity="data.movementType === 1 ? 'success' : data.movementType === 2 ? 'warn' : 'info'" />
      </template></PvColumn>
      <PvColumn field="chemicalName" header="Химия" />
      <PvColumn header="Кол-во, л"><template #body="{ data }">{{ data.quantityLiters }}</template></PvColumn>
      <PvColumn header="Склад"><template #body="{ data }">Склад {{ data.warehouseNumber }}</template></PvColumn>
      <PvColumn field="cropName" header="Культура" />
      <PvColumn header="" style="width: 5rem"><template #body="{ data }">
        <PvButton icon="pi pi-info-circle" text rounded @click="openDetail(data)" />
      </template></PvColumn>
      <template #empty><div class="empty">Операций нет</div></template>
    </PvDataTable>

    <!-- Детали операции -->
    <PvDialog v-model:visible="detailDialog" header="Операция" modal :style="{ width: '30rem' }">
      <div v-if="detail" class="detail">
        <div><b>{{ typeLabel(detail.movementType) }}</b> · {{ fmtDate(detail.occurredAt!) }}</div>
        <div>{{ detail.chemicalName }} — {{ detail.quantityLiters }} л · Склад {{ detail.warehouseNumber }}</div>
        <div v-if="detail.cropName">Культура: {{ detail.cropName }}</div>
        <div v-if="detail.comment">Комментарий: {{ detail.comment }}</div>
        <div v-if="detail.sources?.length" class="sources">
          <b>Источники:</b>
          <ul><li v-for="(s, i) in detail.sources" :key="i">{{ sourceLabel(s.sourceType) }}: {{ s.quantityLiters }} л</li></ul>
        </div>
      </div>
      <template #footer>
        <PvButton label="Удалить" text severity="danger" @click="remove(detail!)" />
        <PvButton label="Редактировать" outlined @click="openEdit" />
        <PvButton label="Закрыть" text @click="detailDialog = false" />
      </template>
    </PvDialog>

    <!-- Редактирование операции -->
    <PvDialog v-model:visible="editDialog" header="Редактировать операцию" modal :style="{ width: '28rem' }">
      <div class="field"><span>Дата и время</span>
        <input class="dt" type="datetime-local" v-model="edit.occurredAtLocal" />
      </div>
      <template v-if="isPackageIncome()">
        <div class="field"><span>Литраж упаковки</span><PvInputText v-model.number="edit.packageVolume" type="number" /></div>
        <div class="field"><span>Количество упаковок</span><PvInputText v-model.number="edit.packagesQuantity" type="number" /></div>
      </template>
      <div v-else class="field"><span>Количество, л</span><PvInputText v-model.number="edit.quantityLiters" type="number" /></div>
      <div v-if="detail?.movementType === MovementType.Outcome" class="field"><span>Культура</span>
        <PvSelect v-model="edit.cropId" :options="ref_.cropOptions.value" option-label="label" option-value="value" filter />
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
.filters { display: flex; gap: 0.5rem; flex-wrap: wrap; }
.mt { margin-top: 1rem; }
.empty { padding: 1rem; color: #6b7280; }
.detail { display: flex; flex-direction: column; gap: 0.4rem; }
.sources ul { margin: 0.25rem 0; padding-left: 1.25rem; }
.field { display: flex; flex-direction: column; gap: 0.25rem; margin-bottom: 0.75rem; }
.field > span { font-weight: 600; font-size: 0.9rem; }
.dt { padding: 0.5rem; border: 1px solid #d1d5db; border-radius: 6px; font: inherit; }
</style>
