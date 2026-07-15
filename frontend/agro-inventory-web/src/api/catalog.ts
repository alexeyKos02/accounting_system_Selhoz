import { apiGet, apiPost, apiPut } from './http'
import type {
  CanonicalChemicalDto, CreateCanonicalChemicalRequest, UpdateCanonicalChemicalRequest,
  AggregatedChemicalGroupDto,
} from './types'

// Общий канонический справочник препаратов (ТЗ §12)
export const canonicalApi = {
  list: (search?: string) =>
    apiGet<CanonicalChemicalDto[]>(`/canonical-chemicals${search ? `?search=${encodeURIComponent(search)}` : ''}`),
  create: (body: CreateCanonicalChemicalRequest) => apiPost<CanonicalChemicalDto>('/canonical-chemicals', body),
  update: (id: string, body: UpdateCanonicalChemicalRequest) =>
    apiPut<CanonicalChemicalDto>(`/canonical-chemicals/${id}`, body),
}

// Химия в режиме «Все хозяйства» — агрегат по каноническому препарату (ТЗ §17)
export const allCompaniesApi = {
  chemicals: () => apiGet<AggregatedChemicalGroupDto[]>('/all-companies/chemicals'),
}
