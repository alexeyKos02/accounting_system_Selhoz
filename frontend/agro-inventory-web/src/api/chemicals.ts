import { apiGet, apiPost, apiPut } from './http'
import type {
  ArchivedChemicalDto,
  ChemicalDetailDto,
  ChemicalListItemDto,
  CreateChemicalRequest,
  DuplicateDto,
  MergeChemicalsRequest,
  UpdateChemicalRequest,
} from './types'

export interface ChemicalListParams {
  search?: string
  cropId?: string
  warehouseId?: string
}

function toQuery(params: ChemicalListParams): string {
  const q = new URLSearchParams()
  if (params.search) q.set('Search', params.search)
  if (params.cropId) q.set('CropId', params.cropId)
  if (params.warehouseId) q.set('WarehouseId', params.warehouseId)
  const s = q.toString()
  return s ? `?${s}` : ''
}

// Химия (ТЗ §15–18)
export const chemicalsApi = {
  list: (params: ChemicalListParams = {}) =>
    apiGet<ChemicalListItemDto[]>(`/chemicals${toQuery(params)}`),
  get: (id: string) => apiGet<ChemicalDetailDto>(`/chemicals/${id}`),
  create: (body: CreateChemicalRequest) => apiPost<ChemicalDetailDto>('/chemicals', body),
  update: (id: string, body: UpdateChemicalRequest) =>
    apiPut<ChemicalDetailDto>(`/chemicals/${id}`, body),
  archive: (id: string, confirmation?: string) =>
    apiPost<void>(`/chemicals/${id}/archive`, { confirmation }),
  restore: (id: string) => apiPost<void>(`/chemicals/${id}/restore`),
  merge: (body: MergeChemicalsRequest) => apiPost<ChemicalDetailDto>('/chemicals/merge', body),
  duplicates: (name: string) =>
    apiGet<DuplicateDto[]>(`/chemicals/duplicates?name=${encodeURIComponent(name)}`),
  archived: () => apiGet<ArchivedChemicalDto[]>('/chemicals/archived'),
}
