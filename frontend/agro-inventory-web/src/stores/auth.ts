import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import { authApi } from '../api/auth'
import { authStorage } from '../api/authStorage'
import type { LoginRequest, MeResponse } from '../api/types'

// Аутентификация текущего пользователя (ТЗ §1). Токены — в localStorage (authStorage),
// профиль (/users/me) — в памяти стора.
export const useAuthStore = defineStore('auth', () => {
  const user = ref<MeResponse | null>(null)
  const loading = ref(false)

  const isAuthenticated = computed(() => !!authStorage.getAccess() && !!user.value)
  const hasToken = computed(() => !!authStorage.getAccess())
  const mustChangePassword = computed(() => user.value?.mustChangePassword ?? false)
  const isSystemAdmin = computed(() => user.value?.isSystemAdmin ?? false)
  const memberships = computed(() => user.value?.memberships ?? [])
  const displayName = computed(() =>
    user.value ? `${user.value.firstName ?? ''} ${user.value.lastName ?? ''}`.trim() || (user.value.email ?? '') : '')

  async function login(body: LoginRequest) {
    const tokens = await authApi.login(body)
    authStorage.setTokens(tokens.accessToken!, tokens.refreshToken!)
    await fetchMe()
  }

  async function fetchMe() {
    loading.value = true
    try {
      user.value = await authApi.me()
    } finally {
      loading.value = false
    }
  }

  async function changePassword(currentPassword: string, newPassword: string) {
    await authApi.changePassword({ currentPassword, newPassword })
    // Пароль сменён — снимаем флаг обязательной смены (ТЗ §1).
    if (user.value) user.value = { ...user.value, mustChangePassword: false }
  }

  async function logout() {
    const refresh = authStorage.getRefresh()
    try {
      if (refresh) await authApi.logout(refresh)
    } catch {
      // выход должен работать даже без сети
    }
    reset()
  }

  function reset() {
    user.value = null
    authStorage.clearTokens()
  }

  return {
    user, loading,
    isAuthenticated, hasToken, mustChangePassword, isSystemAdmin, memberships, displayName,
    login, fetchMe, changePassword, logout, reset,
  }
})
