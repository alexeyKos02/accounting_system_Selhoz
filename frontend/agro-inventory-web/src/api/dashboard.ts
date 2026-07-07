import { apiGet } from './http'
import type { components } from './schema'

type S = components['schemas']
export type DashboardDto = S['DashboardDto']
export type DashboardStockDto = S['DashboardStockDto']

// Сводка для главного экрана (ТЗ §22)
export const dashboardApi = {
  get: () => apiGet<DashboardDto>('/dashboard'),
}
