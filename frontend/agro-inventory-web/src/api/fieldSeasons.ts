import { apiGet, apiPost, apiPut } from './http'

export interface FieldSeasonDto {
  id?: string
  fieldId?: string
  fieldNumber?: string
  cropId?: string
  cropName?: string
  year?: number
  name?: string | null
  startedAt?: string | null
  finishedAt?: string | null
  comment?: string | null
}

export interface CreateFieldSeasonRequest {
  fieldId: string
  cropId: string
  year: number
  name?: string | null
  startedAt?: string | null
  finishedAt?: string | null
  comment?: string | null
}

export type UpdateFieldSeasonRequest = Omit<CreateFieldSeasonRequest, 'fieldId'>

export const fieldSeasonsApi = {
  list: (fieldId?: string) => apiGet<FieldSeasonDto[]>(`/field-seasons${fieldId ? `?fieldId=${fieldId}` : ''}`),
  create: (body: CreateFieldSeasonRequest) => apiPost<FieldSeasonDto>('/field-seasons', body),
  update: (id: string, body: UpdateFieldSeasonRequest) => apiPut<FieldSeasonDto>(`/field-seasons/${id}`, body),
}
