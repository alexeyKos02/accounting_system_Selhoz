import { apiPost } from './http'
import type { components } from './schema'

type S = components['schemas']
export type IncomeRequest = S['IncomeRequest']
export type IncomeResultDto = S['IncomeResultDto']
export type OutcomeRequest = S['OutcomeRequest']
export type OutcomePreviewResponse = S['OutcomePreviewResponse']
export type OutcomeResultDto = S['OutcomeResultDto']
export type CorrectionRequest = S['CorrectionRequest']
export type CorrectionPreviewResponse = S['CorrectionPreviewResponse']
export type CorrectionResultDto = S['CorrectionResultDto']

// Складские операции (ТЗ §9–13)
export const inventoryApi = {
  income: (body: IncomeRequest) => apiPost<IncomeResultDto>('/inventory/income', body),
  previewOutcome: (body: OutcomeRequest) =>
    apiPost<OutcomePreviewResponse>('/inventory/outcome/preview', body),
  outcome: (body: OutcomeRequest) => apiPost<OutcomeResultDto>('/inventory/outcome', body),
  previewCorrection: (body: CorrectionRequest) =>
    apiPost<CorrectionPreviewResponse>('/inventory/correction/preview', body),
  correction: (body: CorrectionRequest) => apiPost<CorrectionResultDto>('/inventory/correction', body),
}

// Единицы измерения (совпадает с Domain.UnitType)
export const Unit = { Liter: 1, Can: 2, Piece: 3 } as const
// Режимы корректировки (Application.CorrectionMode)
export const CorrectionMode = { SetActual: 0, AdjustByDelta: 1, DetailedInventory: 2 } as const
