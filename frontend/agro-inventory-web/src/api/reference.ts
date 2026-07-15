import { apiGet, apiPost, apiPut } from './http'
import type { CropDto, WarehouseDto, FieldDto } from './types'

// Культуры (ТЗ §7.3)
export const cropsApi = {
  list: () => apiGet<CropDto[]>('/crops'),
  create: (name: string) => apiPost<CropDto>('/crops', { name }),
  update: (id: string, name: string) => apiPut<CropDto>(`/crops/${id}`, { name }),
}

// Склады (ТЗ §7.4)
export const warehousesApi = {
  list: () => apiGet<WarehouseDto[]>('/warehouses'),
  create: (number: string) => apiPost<WarehouseDto>('/warehouses', { number }),
  update: (id: string, number: string) => apiPut<WarehouseDto>(`/warehouses/${id}`, { number }),
}

// Поля/участки (справочник для списания)
export const fieldsApi = {
  list: () => apiGet<FieldDto[]>('/fields'),
  get: (id: string) => apiGet<FieldDto>(`/fields/${id}`),
  create: (body: { number: string; areaHectares?: number | null; currentCropId?: string | null }) =>
    apiPost<FieldDto>('/fields', body),
  update: (id: string, body: { number: string; areaHectares?: number | null; currentCropId?: string | null }) =>
    apiPut<FieldDto>(`/fields/${id}`, body),
}
