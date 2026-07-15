import { apiGet } from './http'
import type { components } from './schema'

type S = components['schemas']
export type DashboardDto = S['DashboardDto']
export type DashboardStockDto = S['DashboardStockDto']

export interface AllCompaniesDashboardCompanyDto {
  companyId?: string
  companyName?: string
  activeChemicals?: number
  warehouses?: number
  totalLiters?: number
  incomeLiters?: number
  outcomeLiters?: number
  lowCount?: number
  emptyCount?: number
}

export interface AllCompaniesDashboardAlertDto {
  companyId?: string
  companyName?: string
  chemicalId?: string
  chemicalName?: string
  totalLiters?: number
  status?: number
}

export interface AllCompaniesDashboardDto {
  companiesCount?: number
  activeChemicals?: number
  warehouses?: number
  totalLiters?: number
  incomeLiters?: number
  outcomeLiters?: number
  lowCount?: number
  emptyCount?: number
  lowStockThresholdLiters?: number
  companies?: AllCompaniesDashboardCompanyDto[] | null
  low?: AllCompaniesDashboardAlertDto[] | null
  empty?: AllCompaniesDashboardAlertDto[] | null
  recentOperations?: S['HistoryItemDto'][] | null
}

export interface AllCompaniesDashboardFilters {
  dateFrom?: string
  dateTo?: string
}

function toQuery(params: Record<string, unknown>): string {
  const q = new URLSearchParams()
  for (const [k, v] of Object.entries(params)) {
    if (v !== undefined && v !== null && v !== '') q.set(k[0].toUpperCase() + k.slice(1), String(v))
  }
  const s = q.toString()
  return s ? `?${s}` : ''
}

// Сводка для главного экрана (ТЗ §22)
export const dashboardApi = {
  get: () => apiGet<DashboardDto>('/dashboard'),
  getAll: (filters: AllCompaniesDashboardFilters = {}) =>
    apiGet<AllCompaniesDashboardDto>(`/dashboard/all${toQuery(filters as Record<string, unknown>)}`),
}
