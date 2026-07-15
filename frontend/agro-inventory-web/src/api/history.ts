import { apiGet, apiPut, apiDelete } from './http'
import type { components } from './schema'

type S = components['schemas']
export type HistoryItemDto = S['HistoryItemDto'] & {
  targetWarehouseId?: string | null
  targetWarehouseNumber?: string | null
  fieldTreatmentId?: string | null
}
export type HistoryDetailDto = S['HistoryDetailDto'] & {
  targetWarehouseId?: string | null
  targetWarehouseNumber?: string | null
  fieldTreatmentId?: string | null
}
export type EditMovementRequest = S['EditMovementRequest']
export type AuditLogDto = S['AuditLogDto'] & { companyId?: string | null; companyName?: string | null }

export interface HistoryFilters {
  dateFrom?: string
  dateTo?: string
  chemicalId?: string
  movementType?: number
  warehouseId?: string
  cropId?: string
  fieldId?: string
}

function toQuery(params: Record<string, unknown>): string {
  const q = new URLSearchParams()
  for (const [k, v] of Object.entries(params)) {
    if (v !== undefined && v !== null && v !== '') q.set(k[0].toUpperCase() + k.slice(1), String(v))
  }
  const s = q.toString()
  return s ? `?${s}` : ''
}

// История операций (ТЗ §19, §20)
export const historyApi = {
  list: (f: HistoryFilters = {}) => apiGet<HistoryItemDto[]>(`/history${toQuery(f as Record<string, unknown>)}`),
  get: (id: string) => apiGet<HistoryDetailDto>(`/history/${id}`),
  update: (id: string, body: EditMovementRequest) => apiPut<void>(`/history/${id}`, body),
  remove: (id: string) => apiDelete<void>(`/history/${id}`),
}

// Audit log (ТЗ §21)
export interface AuditFilters {
  dateFrom?: string
  dateTo?: string
  action?: number
  entityType?: string
}
export const auditApi = {
  list: (f: AuditFilters = {}) => apiGet<AuditLogDto[]>(`/audit-log${toQuery(f as Record<string, unknown>)}`),
}

// MovementType: 1 income, 2 outcome, 3 correction, 4 transfer
export const MovementType = { Income: 1, Outcome: 2, Correction: 3, Transfer: 4 } as const
// AuditAction: 1 create, 2 update, 3 delete, 4 restore, 5 archive, 6 merge
export const AuditAction = { Create: 1, Update: 2, Delete: 3, Restore: 4, Archive: 5, Merge: 6 } as const
