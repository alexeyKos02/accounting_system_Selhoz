import { apiGet, apiPost, API_BASE, ApiError, NetworkError } from './http'
import type { components } from './schema'

type S = components['schemas']
export type OperationSuggestionDto = S['OperationSuggestionDto']
export type ChemicalEnrichmentDto = S['ChemicalEnrichmentDto']
export type ReferenceMatchDto = S['ReferenceMatchDto']

// GPT-помощник (ТЗ §26). Возвращает только предложения — ничего не сохраняет.
export const gptApi = {
  status: () => apiGet<{ configured: boolean }>('/gpt/status'),
  parseText: (text: string) => apiPost<OperationSuggestionDto>('/gpt/parse-text', { text }),
  enrichChemical: (name: string) => apiPost<ChemicalEnrichmentDto>('/gpt/enrich-chemical', { name }),

  // Фото — multipart/form-data, поэтому отдельный fetch (не через json-обёртку).
  async parsePhoto(file: File): Promise<OperationSuggestionDto> {
    const form = new FormData()
    form.append('file', file)
    let response: Response
    try {
      response = await fetch(`${API_BASE}/gpt/parse-photo`, { method: 'POST', body: form })
    } catch {
      throw new NetworkError('Нет соединения с сервером')
    }
    if (!response.ok) {
      const text = await response.text().catch(() => '')
      throw new ApiError(response.status, text || response.statusText)
    }
    return (await response.json()) as OperationSuggestionDto
  },
}
