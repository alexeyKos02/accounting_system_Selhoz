import { apiGet } from './http'

export interface ReceiptFilters {
  dateFrom?: string
  dateTo?: string
  companyId?: string
  canonicalChemicalId?: string
}

export interface ReceiptItemDto {
  id?: string
  occurredAt?: string
  companyId?: string
  companyName?: string
  chemicalId?: string
  chemicalName?: string
  canonicalChemicalId?: string | null
  canonicalChemicalName?: string | null
  quantityLiters?: number
  unitType?: number | null
  packageVolumeLiters?: number | null
  packagesQuantity?: number | null
  warehouseId?: string
  warehouseNumber?: string
  comment?: string | null
}

function toQuery(params: Record<string, unknown>): string {
  const q = new URLSearchParams()
  for (const [k, v] of Object.entries(params)) {
    if (v !== undefined && v !== null && v !== '') q.set(k[0].toUpperCase() + k.slice(1), String(v))
  }
  const s = q.toString()
  return s ? `?${s}` : ''
}

export const receiptsApi = {
  list: (filters: ReceiptFilters = {}) =>
    apiGet<ReceiptItemDto[]>(`/receipts${toQuery(filters as Record<string, unknown>)}`),
}
