import { apiGet, apiPost, apiPut } from './http'
import type { CropDto, WarehouseDto } from './types'

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
