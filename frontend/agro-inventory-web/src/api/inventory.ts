import { apiGet, apiPost } from './http'
import type { components } from './schema'

type S = components['schemas']
export type IncomeRequest = S['IncomeRequest']
export type IncomeResultDto = S['IncomeResultDto']
export type OutcomeRequest = S['OutcomeRequest']
export type OutcomePreviewResponse = S['OutcomePreviewResponse']
export type OutcomeResultDto = S['OutcomeResultDto']
export type TransferRequest = {
  chemicalId: string
  sourceWarehouseId: string
  targetWarehouseId: string
  quantityLiters: number
  allowOpenNewPackage: boolean
  occurredAt?: string | null
  comment?: string | null
}
export type TransferResultDto = {
  movementId?: string
  sourceTotalLiters?: number
  targetTotalLiters?: number
}
export type TransferItemDto = {
  id?: string
  occurredAt?: string
  chemicalId?: string
  chemicalName?: string
  sourceWarehouseId?: string
  sourceWarehouseNumber?: string
  targetWarehouseId?: string
  targetWarehouseNumber?: string
  quantityLiters?: number
  comment?: string | null
}
export type CorrectionRequest = S['CorrectionRequest']
export type CorrectionPreviewResponse = S['CorrectionPreviewResponse']
export type CorrectionResultDto = S['CorrectionResultDto']
export type InventoryCheckSheetDto = S['InventoryCheckSheetDto']
export type InventoryCheckLineDto = S['InventoryCheckLineDto']
export type InventoryCheckRequest = S['InventoryCheckRequest']
export type InventoryCheckResultDto = S['InventoryCheckResultDto']
export type InventoryCheckLineResult = S['InventoryCheckLineResult']

export type BulkIncomeWarehouseOptionDto = {
  warehouseId?: string
  warehouseNumber?: string | null
}
export type BulkIncomeCompanyOptionDto = {
  companyId?: string
  companyName?: string | null
  chemicalId?: string | null
  chemicalName?: string | null
  warehouses?: BulkIncomeWarehouseOptionDto[] | null
  blockedReason?: string | null
}
export type BulkIncomeOptionsDto = {
  companies?: BulkIncomeCompanyOptionDto[] | null
}
export type BulkIncomeLineRequest = {
  companyId: string
  warehouseId: string
  quantity: number
}
export type BulkIncomeRequest = {
  canonicalChemicalId: string
  unit: number
  packageVolumeLiters?: number | null
  occurredAt?: string | null
  comment?: string | null
  lines: BulkIncomeLineRequest[]
}
export type BulkIncomeResultDto = {
  lines?: {
    companyId?: string
    chemicalId?: string
    warehouseId?: string
    totalLiters?: number
    movementId?: string
  }[] | null
}

// Складские операции (ТЗ §9–14)
export const inventoryApi = {
  income: (body: IncomeRequest) => apiPost<IncomeResultDto>('/inventory/income', body),
  bulkIncomeOptions: (canonicalChemicalId: string) =>
    apiGet<BulkIncomeOptionsDto>(`/inventory/income/bulk/options?canonicalChemicalId=${canonicalChemicalId}`),
  bulkIncome: (body: BulkIncomeRequest) => apiPost<BulkIncomeResultDto>('/inventory/income/bulk', body),
  previewOutcome: (body: OutcomeRequest) =>
    apiPost<OutcomePreviewResponse>('/inventory/outcome/preview', body),
  outcome: (body: OutcomeRequest) => apiPost<OutcomeResultDto>('/inventory/outcome', body),
  transfers: () => apiGet<TransferItemDto[]>('/inventory/transfers'),
  transfer: (body: TransferRequest) => apiPost<TransferResultDto>('/inventory/transfers', body),
  previewCorrection: (body: CorrectionRequest) =>
    apiPost<CorrectionPreviewResponse>('/inventory/correction/preview', body),
  correction: (body: CorrectionRequest) => apiPost<CorrectionResultDto>('/inventory/correction', body),
  checkSheet: (warehouseId: string) =>
    apiGet<InventoryCheckSheetDto>(`/inventory/check-sheet?warehouseId=${warehouseId}`),
  applyCheck: (body: InventoryCheckRequest) =>
    apiPost<InventoryCheckResultDto>('/inventory/check', body),
}

// Единицы измерения (совпадает с Domain.UnitType)
export const Unit = { Liter: 1, Can: 2, Piece: 3 } as const
// Режимы корректировки (Application.CorrectionMode)
export const CorrectionMode = { SetActual: 0, AdjustByDelta: 1, DetailedInventory: 2 } as const
// Результат строки инвентаризации (Application.InventoryCheckOutcome)
export const CheckOutcome = { Unchanged: 0, Applied: 1, NeedsDetailed: 2 } as const
