import { apiGet, apiPost, apiPut } from './http'
import type { AdminUserDto, CreateUserRequest, UpdateUserRequest } from './types'

// Управление аккаунтами системным администратором (ТЗ §1, §21)
export const adminUsersApi = {
  list: () => apiGet<AdminUserDto[]>('/admin/users'),
  create: (body: CreateUserRequest) => apiPost<AdminUserDto>('/admin/users', body),
  update: (userId: string, body: UpdateUserRequest) => apiPut<AdminUserDto>(`/admin/users/${userId}`, body),
  block: (userId: string, blocked: boolean) =>
    apiPost<AdminUserDto>(`/admin/users/${userId}/block`, { blocked }),
  resetPassword: (userId: string, newPassword: string) =>
    apiPost<void>(`/admin/users/${userId}/reset-password`, { newPassword }),
}
