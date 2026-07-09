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
import { StockStatus, chemicalTypeLabels, chemicalTypeOptions } from '../api/types'

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
const typeId = ref<number | null>(null)
const activeFiltersCount = computed(() =>
  [search.value.trim(), cropId.value, warehouseId.value, typeId.value].filter((v) => v != null && v !== '').length,
)
const filterButtonLabel = computed(() =>
  activeFiltersCount.value ? `Фильтры (${activeFiltersCount.value})` : 'Фильтры',
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
      type: typeId.value ?? undefined,
    })
  } finally {
    loading.value = false
  }
}

watch(search, () => {
  clearTimeout(debounce)
  debounce = setTimeout(load, 300)
})
watch([cropId, warehouseId, typeId], load)

function badge(status?: number): { label: string; severity: 'danger' | 'warn' } | null {
  if (status === StockStatus.Empty) return { label: 'Закончилась', severity: 'danger' }
  if (status === StockStatus.Low) return { label: 'Малый остаток', severity: 'warn' }
  return null
}

const cropOptions = computed(() => crops.value.map((c) => ({ label: c.name, value: c.id })))
const warehouseOptions = computed(() =>
  warehouses.value.map((w) => ({ label: `Склад ${w.number}`, value: w.id })),
)

function openCard(item: ChemicalListItemDto) {
  router.push({ name: 'chemical-detail', params: { id: item.id! } })
}

function openIncome(item: ChemicalListItemDto) {
  router.push({ name: 'income', query: { chemicalId: item.id } })
}

function openOutcome(item: ChemicalListItemDto) {
  router.push({ name: 'outcome', query: { chemicalId: item.id } })
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
    <div class="toolbar">
    <div class="head">
      <h1 class="page__title">Химия</h1>
      <div class="head__actions head__actions--desktop">
        <PvButton
          :label="filterButtonLabel"
          icon="pi pi-filter"
          :outlined="!filtersVisible"
          :severity="activeFiltersCount ? 'info' : undefined"
          :aria-expanded="filtersVisible"
          aria-controls="chemical-filters"
          @click="filtersVisible = !filtersVisible"
        />
        <PvButton label="Excel" icon="pi pi-file-excel" outlined :loading="exporting" @click="exportExcel" />
        <PvButton label="Добавить химию" icon="pi pi-plus" @click="router.push({ name: 'chemical-create' })" />
      </div>
      <div class="head__actions head__actions--mobile">
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
      <PvSelect v-model="typeId" :options="chemicalTypeOptions" option-label="label" option-value="value"
        placeholder="Тип средства" show-clear />
      <PvSelect v-model="cropId" :options="cropOptions" option-label="label" option-value="value"
        placeholder="Культура" show-clear />
      <PvSelect v-model="warehouseId" :options="warehouseOptions" option-label="label" option-value="value"
        placeholder="Склад" show-clear />
    </div>
    </div>

    <div class="desktop-table-panel mt">
      <div class="desktop-table-panel__head">
        <h2>Список химии</h2>
        <span>Найдено: {{ items.length }}</span>
      </div>
      <PvDataTable :value="items" :loading="loading" data-key="id" class="desktop-table"
        selection-mode="single" @row-click="onRowClick">
        <PvColumn field="name" header="Название" />
        <PvColumn header="Тип">
          <template #body="{ data }">
            <span v-if="data.type">{{ chemicalTypeLabels[data.type] }}</span>
            <span v-else class="muted">—</span>
          </template>
        </PvColumn>
        <PvColumn header="Остаток">
          <template #body="{ data }">
            <div class="stock-cell">
              <span>{{ (data.totalLiters ?? 0).toLocaleString('ru-RU') }} л</span>
              <span
                v-if="badge(data.stockStatus)"
                class="stock-label"
                :class="`stock-label--${badge(data.stockStatus)!.severity}`"
              >
                {{ badge(data.stockStatus)!.label }}
              </span>
            </div>
          </template>
        </PvColumn>
        <PvColumn header="Культуры">
          <template #body="{ data }">
            <div class="desktop-crops">
              <span v-for="crop in data.crops ?? []" :key="crop.id!" class="desktop-crop">{{ crop.name }}</span>
              <span v-if="!data.crops?.length" class="muted">—</span>
            </div>
          </template>
        </PvColumn>
        <PvColumn header="" class="desktop-actions-column">
          <template #body="{ data }">
            <div class="desktop-row-actions">
              <PvButton
                icon="pi pi-plus"
                text
                rounded
                size="small"
                aria-label="Приход"
                @click.stop="openIncome(data)"
              />
              <PvButton
                icon="pi pi-minus"
                text
                rounded
                size="small"
                aria-label="Списание"
                @click.stop="openOutcome(data)"
              />
            </div>
          </template>
        </PvColumn>
        <template #empty><div class="empty">Химия не найдена</div></template>
      </PvDataTable>
    </div>

    <div class="chemical-cards">
      <div v-if="loading" class="empty">Загрузка...</div>
      <article
        v-for="item in items"
        v-else
        :key="item.id!"
        class="chemical-card"
        role="button"
        tabindex="0"
        @click="openCard(item)"
        @keydown.enter="openCard(item)"
        @keydown.space.prevent="openCard(item)"
      >
        <div class="chemical-card__top">
          <div>
            <div class="chemical-card__name">{{ item.name }}</div>
            <div class="chemical-card__type" v-if="item.type">{{ chemicalTypeLabels[item.type] }}</div>
            <div class="chemical-card__cultures" v-if="item.crops?.length">
              <span v-for="crop in item.crops" :key="crop.id!" class="chemical-card__culture">{{ crop.name }}</span>
            </div>
          </div>
          <div class="chemical-card__stock">
            <div>{{ (item.totalLiters ?? 0).toLocaleString('ru-RU') }} л</div>
            <PvTag v-if="badge(item.stockStatus)" :value="badge(item.stockStatus)!.label"
              :severity="badge(item.stockStatus)!.severity" />
          </div>
        </div>
        <div class="chemical-card__actions">
          <PvButton label="Приход" icon="pi pi-plus" text size="small" @click.stop="openIncome(item)" />
          <PvButton label="Списание" icon="pi pi-minus" text size="small" @click.stop="openOutcome(item)" />
          <PvButton label="Карточка" icon="pi pi-info-circle" text size="small" @click.stop="openCard(item)" />
        </div>
      </article>
      <div v-if="!loading && !items.length" class="empty">Химия не найдена</div>
    </div>
  </section>
</template>

<style scoped>
.head { display: flex; justify-content: space-between; align-items: center; gap: 1rem; }
.head__actions { display: flex; gap: 0.5rem; align-items: center; }
.head__actions--mobile { display: none; }
.filters { display: flex; gap: 0.5rem; flex-wrap: wrap; margin-top: 0.5rem; }
.mt { margin-top: 1rem; }
.ml { margin-left: 0.5rem; }
.muted { color: #6b7280; }
.empty { padding: 1rem; color: #6b7280; }
.chemical-cards { display: none; }
.desktop-table-panel {
  overflow: hidden;
  border: 1px solid #e5e7eb;
  border-radius: 8px;
  background: #fff;
}
.desktop-table-panel__head {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 1rem;
  padding: 1.25rem 1.5rem;
  border-bottom: 1px solid #e5e7eb;
}
.desktop-table-panel__head h2 { margin: 0; font-size: 1.2rem; }
.desktop-table-panel__head span { color: #374151; font-weight: 600; }
.stock-cell { display: flex; flex-direction: column; align-items: flex-start; gap: 0.3rem; }
.stock-label {
  display: inline-flex;
  align-items: center;
  min-height: 1.45rem;
  padding: 0.18rem 0.5rem;
  border: 1px solid transparent;
  border-radius: 6px;
  font-size: 0.85rem;
  font-weight: 600;
  line-height: 1.1;
}
.stock-label--danger {
  border-color: #fecaca;
  background: #fff1f2;
  color: #b91c1c;
}
.stock-label--warn {
  border-color: #fed7aa;
  background: #fff7ed;
  color: #c2410c;
}
.desktop-crops { display: flex; flex-wrap: wrap; gap: 0.5rem; }
.desktop-crop {
  padding: 0.35rem 0.7rem;
  border: 1px solid #e5e7eb;
  border-radius: 8px;
  background: #fff;
  color: #374151;
  box-shadow: 0 1px 2px rgb(15 23 42 / 0.04);
}
.desktop-row-actions { display: flex; justify-content: flex-end; gap: 0.25rem; }
:deep(.desktop-actions-column) { width: 6rem; text-align: right; }
:deep(.p-datatable-tbody > tr) { cursor: pointer; }
:deep(.desktop-table .p-datatable-header) { border: 0; }
:deep(.desktop-table .p-datatable-thead > tr > th) {
  padding: 1.15rem 1.5rem;
  border-color: #e5e7eb;
  background: #fff;
  color: #374151;
  font-size: 1rem;
}
:deep(.desktop-table .p-datatable-tbody > tr > td) {
  padding: 1.5rem;
  border-color: #e5e7eb;
  color: #374151;
  vertical-align: middle;
}
:deep(.desktop-table .p-datatable-tbody > tr:hover) { background: #f8fffb; }
:deep(.desktop-table .p-datatable-tbody > tr:hover > td:first-child) {
  box-shadow: inset 3px 0 0 #10b981;
}

@media (max-width: 640px) {
  .head__actions--desktop { display: none; }
  .head__actions--mobile { display: flex; }
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
  .desktop-table-panel { display: none; }
  .chemical-cards { display: flex; flex-direction: column; gap: 0.75rem; margin-top: 1rem; }
  .chemical-card {
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
    width: 100%;
    padding: 0.875rem;
    border: 1px solid #e5e7eb;
    border-radius: 8px;
    background: #fff;
    color: inherit;
    font: inherit;
    text-align: left;
  }
  .chemical-card__top { display: flex; justify-content: space-between; gap: 1rem; }
  .chemical-card__name { font-weight: 700; color: #374151; }
  .chemical-card__type { margin-top: 0.15rem; font-size: 0.8rem; color: #6b7280; }
  .chemical-card__cultures { display: flex; flex-wrap: wrap; gap: 0.35rem; margin-top: 0.45rem; }
  .chemical-card__culture {
    padding: 0.15rem 0.45rem;
    border-radius: 999px;
    background: #f3f4f6;
    color: #4b5563;
    font-size: 0.78rem;
  }
  .chemical-card__stock {
    display: flex;
    flex-direction: column;
    align-items: flex-end;
    gap: 0.35rem;
    flex: 0 0 auto;
    font-weight: 700;
    color: #374151;
  }
  .chemical-card__actions {
    display: flex;
    justify-content: space-between;
    gap: 0.25rem;
    padding-top: 0.5rem;
    border-top: 1px solid #eef2f7;
  }
  .chemical-card__actions :deep(.p-button) { padding-inline: 0.35rem; }
}
</style>
