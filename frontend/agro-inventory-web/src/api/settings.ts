import { apiGet, apiPut } from './http'
import type { components } from './schema'

type S = components['schemas']
export type AppSettingsDto = S['AppSettingsDto']
export type UpdateSettingsRequest = S['UpdateSettingsRequest']

// Глобальные настройки (ТЗ §23)
export const settingsApi = {
  get: () => apiGet<AppSettingsDto>('/settings'),
  update: (body: UpdateSettingsRequest) => apiPut<AppSettingsDto>('/settings', body),
}
