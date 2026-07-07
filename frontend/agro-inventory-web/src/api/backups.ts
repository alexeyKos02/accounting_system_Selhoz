import { apiGet, apiPost, apiDownload } from './http'
import type { components } from './schema'

type S = components['schemas']
export type BackupInfo = S['BackupInfo']
export type BackupRestoreResultDto = S['BackupRestoreResultDto']

export interface BackupsListResponse {
  configured: boolean
  backups: BackupInfo[]
}

// Резервные копии БД (ТЗ §24)
export const backupsApi = {
  list: () => apiGet<BackupsListResponse>('/backups'),
  create: () => apiPost<BackupInfo>('/backups'),
  restore: (fileName: string) =>
    apiPost<BackupRestoreResultDto>(`/backups/${encodeURIComponent(fileName)}/restore`),
  download: (fileName: string) =>
    apiDownload(`/backups/${encodeURIComponent(fileName)}/download`, fileName),
}
