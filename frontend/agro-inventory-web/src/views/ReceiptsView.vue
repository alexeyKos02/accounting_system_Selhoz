<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { canonicalApi } from '../api/catalog'
import { companiesApi } from '../api/companies'
import { receiptsApi } from '../api/receipts'
import type { ReceiptFilters, ReceiptItemDto } from '../api/receipts'
import type { CanonicalChemicalDto, CompanyListItemDto } from '../api/types'
import { UnitType } from '../api/types'
import { ApiError } from '../api/http'
import { useToast } from 'primevue/usetoast'

const toast = useToast()
const items = ref<ReceiptItemDto[]>([])
const companies = ref<CompanyListItemDto[]>([])
const canonicalChemicals = ref<CanonicalChemicalDto[]>([])
const filters = ref<ReceiptFilters>({})
const loading = ref(false)
const filtersVisible = ref(false)

const companyOptions = computed(() =>
  companies.value.map((c) => ({ label: c.name, value: c.id })),
)
const canonicalOptions = computed(() =>
  canonicalChemicals.value.map((c) => ({ label: c.canonicalName, value: c.id })),
)
const activeFiltersCount = computed(() =>
  [filters.value.dateFrom, filters.value.dateTo, filters.value.companyId, filters.value.canonicalChemicalId]
    .filter((v) => v !== undefined && v !== null && v !== '').length,
)
const filterBadge = computed(() => (activeFiltersCount.value ? String(activeFiltersCount.value) : undefined))

let timer: ReturnType<typeof setTimeout> | undefined

function fmtDate(iso?: string) {
  return iso ? new Date(iso).toLocaleString('ru-RU') : '—'
}
function fmtNum(n?: number) {
  return (n ?? 0).toLocaleString('ru-RU', { maximumFractionDigits: 3 })
}
function unitLabel(item: ReceiptItemDto) {
  if (item.unitType === UnitType.Can) return `${item.packagesQuantity ?? 0} бан. × ${fmtNum(item.packageVolumeLiters ?? 0)} л`
  if (item.unitType === UnitType.Piece) return `${item.packagesQuantity ?? 0} шт. × ${fmtNum(item.packageVolumeLiters ?? 0)} л`
  return `${fmtNum(item.quantityLiters)} л`
}
function fail(e: unknown) {
  toast.add({
    severity: 'error',
    summary: 'Ошибка',
    detail: e instanceof ApiError ? e.message : 'Не удалось загрузить приходы',
    life: 4000,
  })
}

async function load() {
  loading.value = true
  try {
    items.value = await receiptsApi.list(filters.value)
  } catch (e) {
    fail(e)
  } finally {
    loading.value = false
  }
}

function clearFilters() {
  filters.value = {}
}

watch(filters, () => {
  clearTimeout(timer)
  timer = setTimeout(load, 300)
}, { deep: true })

onMounted(async () => {
  ;[companies.value, canonicalChemicals.value] = await Promise.all([
    companiesApi.list(),
    canonicalApi.list(),
  ])
  await load()
})
</script>

<template>
  <section class="page">
    <div class="toolbar">
      <div class="head">
        <h1 class="page__title">Приходы</h1>
        <div class="head__actions">
          <PvButton
            class="filter-toggle"
            icon="pi pi-filter"
            rounded
            :outlined="!filtersVisible"
            :severity="activeFiltersCount ? 'info' : undefined"
            :badge="filterBadge"
            aria-label="Фильтры"
            :aria-expanded="filtersVisible"
            aria-controls="receipt-filters"
            @click="filtersVisible = !filtersVisible"
          />
          <PvButton label="Сбросить" icon="pi pi-times" text :disabled="!activeFiltersCount" @click="clearFilters" />
        </div>
      </div>

      <div id="receipt-filters" class="filters" :class="{ 'filters--open': filtersVisible }">
        <input v-model="filters.dateFrom" class="dt" type="date" aria-label="Дата от" />
        <input v-model="filters.dateTo" class="dt" type="date" aria-label="Дата до" />
        <PvSelect
          v-model="filters.companyId"
          :options="companyOptions"
          option-label="label"
          option-value="value"
          show-clear
          filter
          placeholder="Хозяйство"
        />
        <PvSelect
          v-model="filters.canonicalChemicalId"
          :options="canonicalOptions"
          option-label="label"
          option-value="value"
          show-clear
          filter
          placeholder="Препарат"
        />
      </div>
    </div>

    <div class="desktop-table mt">
      <PvDataTable :value="items" :loading="loading" data-key="id">
        <PvColumn header="Дата"><template #body="{ data }">{{ fmtDate(data.occurredAt) }}</template></PvColumn>
        <PvColumn field="companyName" header="Хозяйство" />
        <PvColumn header="Препарат">
          <template #body="{ data }">
            <div class="chem">
              <span>{{ data.chemicalName }}</span>
              <span v-if="data.canonicalChemicalName" class="muted">{{ data.canonicalChemicalName }}</span>
            </div>
          </template>
        </PvColumn>
        <PvColumn header="Количество"><template #body="{ data }">{{ unitLabel(data) }}</template></PvColumn>
        <PvColumn header="Всего, л"><template #body="{ data }">{{ fmtNum(data.quantityLiters) }} л</template></PvColumn>
        <PvColumn header="Склад"><template #body="{ data }">Склад {{ data.warehouseNumber }}</template></PvColumn>
        <PvColumn field="comment" header="Комментарий" />
        <template #empty><div class="empty">Приходов нет</div></template>
      </PvDataTable>
    </div>

    <div class="receipt-cards">
      <div v-if="loading" class="empty">Загрузка…</div>
      <template v-else>
        <article v-for="item in items" :key="item.id!" class="receipt-card">
          <div class="receipt-card__top">
            <span class="receipt-card__company">{{ item.companyName }}</span>
            <span class="receipt-card__date">{{ fmtDate(item.occurredAt) }}</span>
          </div>
          <div class="receipt-card__name">{{ item.chemicalName }}</div>
          <div v-if="item.canonicalChemicalName" class="receipt-card__canonical">{{ item.canonicalChemicalName }}</div>
          <div class="receipt-card__meta">
            <span class="receipt-card__qty">{{ unitLabel(item) }}</span>
            <span>{{ fmtNum(item.quantityLiters) }} л</span>
            <span>Склад {{ item.warehouseNumber }}</span>
          </div>
          <div v-if="item.comment" class="receipt-card__comment">{{ item.comment }}</div>
        </article>
        <div v-if="!items.length" class="empty">Приходов нет</div>
      </template>
    </div>
  </section>
</template>

<style scoped>
.head { display: flex; justify-content: space-between; align-items: center; gap: 1rem; }
.head__actions { display: flex; gap: 0.5rem; align-items: center; }
.filters { display: flex; gap: 0.5rem; flex-wrap: wrap; }
.filter-toggle { display: none; }
.mt { margin-top: 1rem; }
.muted { color: #6b7280; font-size: 0.85rem; }
.empty { padding: 1rem; color: #6b7280; }
.chem { display: flex; flex-direction: column; gap: 0.15rem; }
.dt {
  min-height: 2.35rem;
  padding: 0.5rem;
  border: 1px solid var(--p-inputtext-border-color, #d1d5db);
  border-radius: 6px;
  font: inherit;
}
.receipt-cards { display: none; }

@media (max-width: 640px) {
  .filter-toggle { display: inline-flex; }
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
  .filters :deep(.p-select), .dt { width: 100%; min-width: 0; max-width: 100%; box-sizing: border-box; }
  .desktop-table { display: none; }
  .receipt-cards { display: flex; flex-direction: column; gap: 0.75rem; margin-top: 1rem; }
  .receipt-card {
    display: flex;
    flex-direction: column;
    gap: 0.45rem;
    width: 100%;
    padding: 0.875rem;
    border: 1px solid #e5e7eb;
    border-radius: 10px;
    background: #fff;
  }
  .receipt-card__top { display: flex; justify-content: space-between; align-items: center; gap: 0.75rem; }
  .receipt-card__company { font-weight: 700; color: #374151; }
  .receipt-card__date { color: #6b7280; font-size: 0.85rem; text-align: right; }
  .receipt-card__name { font-weight: 700; color: #111827; }
  .receipt-card__canonical { color: #6b7280; font-size: 0.85rem; }
  .receipt-card__meta { display: flex; flex-wrap: wrap; gap: 0.35rem 0.75rem; color: #4b5563; font-size: 0.9rem; }
  .receipt-card__qty { font-weight: 700; color: #16a34a; }
  .receipt-card__comment { color: #6b7280; font-size: 0.9rem; }
}
</style>
