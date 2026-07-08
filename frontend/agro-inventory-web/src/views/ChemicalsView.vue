<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import { useToast } from 'primevue/usetoast'
import Menu from 'primevue/menu'
import type { MenuItem } from 'primevue/menuitem'
import { chemicalsApi } from '../api/chemicals'
import { cropsApi, warehousesApi } from '../api/reference'
import { exportApi } from '../api/export'
import { ApiError } from '../api/http'
import type { ChemicalListItemDto, CropDto, WarehouseDto } from '../api/types'
import { StockStatus } from '../api/types'

const router = useRouter()
const toast = useToast()
const items = ref<ChemicalListItemDto[]>([])
const crops = ref<CropDto[]>([])
const warehouses = ref<WarehouseDto[]>([])
const loading = ref(false)
const exporting = ref(false)
const filtersVisible = ref(false)
const actionsMenu = ref<InstanceType<typeof Menu> | null>(null)

async function exportExcel() {
  exporting.value = true
  try {
    await exportApi.chemicals()
  } catch (e) {
    toast.add({
      severity: 'error', summary: 'Ошибка',
      detail: e instanceof ApiError ? e.message : 'Не удалось выгрузить Excel', life: 4000,
    })
  } finally {
    exporting.value = false
  }
}

const search = ref('')
const cropId = ref<string | null>(null)
const warehouseId = ref<string | null>(null)
const activeFiltersCount = computed(() =>
  [search.value.trim(), cropId.value, warehouseId.value].filter(Boolean).length,
)
const filterButtonBadge = computed(() =>
  activeFiltersCount.value ? String(activeFiltersCount.value) : undefined,
)
const actionItems = computed<MenuItem[]>(() => [
  {
    label: 'Excel',
    icon: 'pi pi-file-excel',
    command: exportExcel,
  },
  {
    label: 'Добавить химию',
    icon: 'pi pi-plus',
    command: () => router.push({ name: 'chemical-create' }),
  },
])

let debounce: ReturnType<typeof setTimeout> | undefined

async function load() {
  loading.value = true
  try {
    items.value = await chemicalsApi.list({
      search: search.value.trim() || undefined,
      cropId: cropId.value ?? undefined,
      warehouseId: warehouseId.value ?? undefined,
    })
  } finally {
    loading.value = false
  }
}

watch(search, () => {
  clearTimeout(debounce)
  debounce = setTimeout(load, 300)
})
watch([cropId, warehouseId], load)

function badge(status: number): { label: string; severity: 'danger' | 'warn' } | null {
  if (status === StockStatus.Empty) return { label: 'Закончилась', severity: 'danger' }
  if (status === StockStatus.Low) return { label: 'Малый остаток', severity: 'warn' }
  return null
}

function cropsLabel(item: ChemicalListItemDto): string {
  const names = (item.crops ?? []).map((c) => c.name)
  const head = names.slice(0, 2).join(', ')
  const more = names.length - 2
  return more > 0 ? `${head} +ещё ${more}` : head
}

const cropOptions = computed(() => crops.value.map((c) => ({ label: c.name, value: c.id })))
const warehouseOptions = computed(() =>
  warehouses.value.map((w) => ({ label: `Склад ${w.number}`, value: w.id })),
)

function openCard(item: ChemicalListItemDto) {
  router.push({ name: 'chemical-detail', params: { id: item.id! } })
}

function onRowClick(event: { data: ChemicalListItemDto }) {
  openCard(event.data)
}

function toggleActions(event: MouseEvent) {
  actionsMenu.value?.toggle(event)
}

onMounted(async () => {
  ;[crops.value, warehouses.value] = await Promise.all([cropsApi.list(), warehousesApi.list()])
  await load()
})
</script>

<template>
  <section class="page">
    <div class="head">
      <h1 class="page__title">Химия</h1>
      <div class="head__actions">
        <PvButton
          icon="pi pi-filter"
          rounded
          :outlined="!filtersVisible"
          :severity="activeFiltersCount ? 'info' : undefined"
          :badge="filterButtonBadge"
          aria-label="Фильтры"
          :aria-expanded="filtersVisible"
          aria-controls="chemical-filters"
          @click="filtersVisible = !filtersVisible"
        />
        <PvButton
          icon="pi pi-ellipsis-v"
          rounded
          outlined
          :loading="exporting"
          aria-label="Действия"
          aria-haspopup="true"
          aria-controls="chemical-actions-menu"
          @click="toggleActions"
        />
        <Menu id="chemical-actions-menu" ref="actionsMenu" :model="actionItems" popup />
      </div>
    </div>

    <div v-show="filtersVisible" id="chemical-filters" class="filters">
      <PvInputText v-model="search" placeholder="Поиск по названию" />
      <PvSelect v-model="cropId" :options="cropOptions" option-label="label" option-value="value"
        placeholder="Культура" show-clear />
      <PvSelect v-model="warehouseId" :options="warehouseOptions" option-label="label" option-value="value"
        placeholder="Склад" show-clear />
    </div>

    <PvDataTable :value="items" :loading="loading" data-key="id" class="mt"
      selection-mode="single" @row-click="onRowClick">
      <PvColumn field="name" header="Название" />
      <PvColumn header="Остаток">
        <template #body="{ data }">
          <span>{{ (data.totalLiters ?? 0).toLocaleString('ru-RU') }} л</span>
          <PvTag v-if="badge(data.stockStatus)" :value="badge(data.stockStatus)!.label"
            :severity="badge(data.stockStatus)!.severity" class="ml" />
        </template>
      </PvColumn>
      <PvColumn header="Культуры">
        <template #body="{ data }">{{ cropsLabel(data) }}</template>
      </PvColumn>
      <template #empty><div class="empty">Химия не найдена</div></template>
    </PvDataTable>
  </section>
</template>

<style scoped>
.head { display: flex; justify-content: space-between; align-items: center; gap: 1rem; }
.head__actions { display: flex; gap: 0.5rem; align-items: center; }
.filters { display: flex; gap: 0.5rem; flex-wrap: wrap; margin-top: 0.5rem; }
.mt { margin-top: 1rem; }
.ml { margin-left: 0.5rem; }
.empty { padding: 1rem; color: #6b7280; }
:deep(.p-datatable-tbody > tr) { cursor: pointer; }
</style>
