// Хранение токенов и выбранного хозяйства в localStorage (ТЗ §1, §15).
// Отдельный модуль, чтобы http.ts и Pinia-сторы не создавали циклических зависимостей.

const ACCESS_KEY = 'agro.accessToken'
const REFRESH_KEY = 'agro.refreshToken'
const COMPANY_KEY = 'agro.selectedCompanyId'

export const authStorage = {
  getAccess: () => localStorage.getItem(ACCESS_KEY),
  getRefresh: () => localStorage.getItem(REFRESH_KEY),
  setTokens(access: string, refresh: string) {
    localStorage.setItem(ACCESS_KEY, access)
    localStorage.setItem(REFRESH_KEY, refresh)
  },
  clearTokens() {
    localStorage.removeItem(ACCESS_KEY)
    localStorage.removeItem(REFRESH_KEY)
  },

  // Выбранное хозяйство (переключатель, ТЗ §15). null — режим «Все хозяйства».
  getCompanyId: () => localStorage.getItem(COMPANY_KEY),
  setCompanyId(id: string | null) {
    if (id) localStorage.setItem(COMPANY_KEY, id)
    else localStorage.removeItem(COMPANY_KEY)
  },
}
