import { computed, ref } from 'vue'
import { cropsApi, warehousesApi } from '../api/reference'
import { chemicalsApi } from '../api/chemicals'
import type { ChemicalListItemDto, CropDto, WarehouseDto } from '../api/types'

/** Загружает справочники для селектов форм (химия/склады/культуры) и даёт опции. */
export function useReference() {
  const chemicals = ref<ChemicalListItemDto[]>([])
  const warehouses = ref<WarehouseDto[]>([])
  const crops = ref<CropDto[]>([])

  async function load() {
    ;[chemicals.value, warehouses.value, crops.value] = await Promise.all([
      chemicalsApi.list(),
      warehousesApi.list(),
      cropsApi.list(),
    ])
  }

  async function reloadWarehouses() {
    warehouses.value = await warehousesApi.list()
  }
  async function reloadCrops() {
    crops.value = await cropsApi.list()
  }

  const chemicalOptions = computed(() => chemicals.value.map((c) => ({ label: c.name!, value: c.id! })))
  const warehouseOptions = computed(() =>
    warehouses.value.map((w) => ({ label: `Склад ${w.number}`, value: w.id! })),
  )
  const cropOptions = computed(() => crops.value.map((c) => ({ label: c.name!, value: c.id! })))

  return {
    chemicals, warehouses, crops,
    chemicalOptions, warehouseOptions, cropOptions,
    load, reloadWarehouses, reloadCrops,
  }
}
