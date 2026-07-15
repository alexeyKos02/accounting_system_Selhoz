import { apiGet, apiPost } from './http'
import type { LoginRequest, TokenResponse, ChangePasswordRequest, MeResponse } from './types'

// Аутентификация (ТЗ §1, §21)
export const authApi = {
  login: (body: LoginRequest) => apiPost<TokenResponse>('/auth/login', body),
  logout: (refreshToken: string) => apiPost<void>('/auth/logout', { refreshToken }),
  logoutAll: () => apiPost<void>('/auth/logout-all'),
  changePassword: (body: ChangePasswordRequest) => apiPost<void>('/auth/change-password', body),
  me: () => apiGet<MeResponse>('/users/me'),
}
