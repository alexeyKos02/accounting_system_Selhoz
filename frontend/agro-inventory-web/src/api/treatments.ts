import { apiGet, apiPost } from './http'

export interface FieldTreatmentDto {
  id?: string
  treatedAt?: string
  fieldId?: string
  fieldNumber?: string
  chemicalId?: string
  chemicalName?: string
  warehouseId?: string
  warehouseNumber?: string
  cropId?: string
  cropName?: string
  measureUnit?: number
  quantity?: number
  ratePerHectare?: number | null
  movementId?: string
  comment?: string | null
}

export interface CreateFieldTreatmentRequest {
  fieldId: string
  chemicalId: string
  warehouseId: string
  cropId: string
  quantity?: number | null
  ratePerHectare?: number | null
  treatedAt?: string | null
  comment?: string | null
}

export const treatmentsApi = {
  list: (fieldId?: string) =>
    apiGet<FieldTreatmentDto[]>(`/field-treatments${fieldId ? `?fieldId=${fieldId}` : ''}`),
  create: (body: CreateFieldTreatmentRequest) => apiPost<FieldTreatmentDto>('/field-treatments', body),
}
