import { computed, ref } from 'vue'
import { cropsApi, warehousesApi, fieldsApi } from '../api/reference'
import { chemicalsApi } from '../api/chemicals'
import type { ChemicalListItemDto, CropDto, WarehouseDto, FieldDto } from '../api/types'

/** Загружает справочники для селектов форм (химия/склады/культуры/поля) и даёт опции. */
export function useReference() {
  const chemicals = ref<ChemicalListItemDto[]>([])
  const warehouses = ref<WarehouseDto[]>([])
  const crops = ref<CropDto[]>([])
  const fields = ref<FieldDto[]>([])

  async function load() {
    ;[chemicals.value, warehouses.value, crops.value, fields.value] = await Promise.all([
      chemicalsApi.list(),
      warehousesApi.list(),
      cropsApi.list(),
      fieldsApi.list(),
    ])
  }

  async function reloadWarehouses() {
    warehouses.value = await warehousesApi.list()
  }
  async function reloadCrops() {
    crops.value = await cropsApi.list()
  }
  async function reloadFields() {
    fields.value = await fieldsApi.list()
  }

  const chemicalOptions = computed(() => chemicals.value.map((c) => ({ label: c.name!, value: c.id! })))
  /** Единица измерения выбранной химии (1 л / 2 кг), для подписей количества. */
  function chemicalUnit(id?: string | null): number | undefined {
    return chemicals.value.find((c) => c.id === id)?.measureUnit
  }
  const warehouseOptions = computed(() =>
    warehouses.value.map((w) => ({ label: `Склад ${w.number}`, value: w.id! })),
  )
  const cropOptions = computed(() => crops.value.map((c) => ({ label: c.name!, value: c.id! })))
  const fieldOptions = computed(() => fields.value.map((f) => ({ label: f.number!, value: f.id! })))

  return {
    chemicals, warehouses, crops, fields,
    chemicalOptions, warehouseOptions, cropOptions, fieldOptions, chemicalUnit,
    load, reloadWarehouses, reloadCrops, reloadFields,
  }
}
