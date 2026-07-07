<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import { chemicalsApi } from '../api/chemicals'
import { cropsApi, warehousesApi } from '../api/reference'
import type { ChemicalListItemDto, CropDto, WarehouseDto } from '../api/types'
import { StockStatus } from '../api/types'

const router = useRouter()
const items = ref<ChemicalListItemDto[]>([])
const crops = ref<CropDto[]>([])
const warehouses = ref<WarehouseDto[]>([])
const loading = ref(false)

const search = ref('')
const cropId = ref<string | null>(null)
const warehouseId = ref<string | null>(null)

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

onMounted(async () => {
  ;[crops.value, warehouses.value] = await Promise.all([cropsApi.list(), warehousesApi.list()])
  await load()
})
</script>

<template>
  <section class="page">
    <div class="head">
      <h1 class="page__title">Химия</h1>
      <PvButton label="Добавить химию" icon="pi pi-plus" @click="router.push({ name: 'chemical-create' })" />
    </div>

    <div class="filters">
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
.filters { display: flex; gap: 0.5rem; flex-wrap: wrap; margin-top: 0.5rem; }
.mt { margin-top: 1rem; }
.ml { margin-left: 0.5rem; }
.empty { padding: 1rem; color: #6b7280; }
:deep(.p-datatable-tbody > tr) { cursor: pointer; }
</style>
