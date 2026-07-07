import { apiDownload } from './http'
import type { HistoryFilters } from './history'

function toQuery(params: Record<string, unknown>): string {
  const q = new URLSearchParams()
  for (const [k, v] of Object.entries(params)) {
    if (v !== undefined && v !== null && v !== '') q.set(k[0].toUpperCase() + k.slice(1), String(v))
  }
  const s = q.toString()
  return s ? `?${s}` : ''
}

// Экспорт в Excel (ТЗ §25)
export const exportApi = {
  chemicals: () => apiDownload('/export/chemicals', 'ostatki-himii.xlsx'),
  history: (f: HistoryFilters = {}) =>
    apiDownload(`/export/history${toQuery(f as Record<string, unknown>)}`, 'istoriya-operaciy.xlsx'),
}
