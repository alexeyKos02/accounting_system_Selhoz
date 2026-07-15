import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import { companiesApi } from '../api/companies'
import { authStorage } from '../api/authStorage'
import { Permissions } from '../api/types'
import type { CompanyListItemDto } from '../api/types'
import { useAuthStore } from './auth'

const ALL_PERMISSIONS = Object.values(Permissions) as string[]

// Контекст хозяйства (ТЗ §15, §22): доступные хозяйства, выбранное, права в текущем хозяйстве.
// Выбранное хозяйство отправляется в заголовке X-Company-Id (см. http.ts).
export const useCompanyContextStore = defineStore('companyContext', () => {
  const auth = useAuthStore()

  const availableCompanies = ref<CompanyListItemDto[]>([])
  const selectedCompanyId = ref<string | null>(authStorage.getCompanyId())
  const loaded = ref(false)

  // «Все хозяйства» — режим без выбранного хозяйства (общий просмотр, ТЗ §15).
  const isAllCompaniesMode = computed(() => selectedCompanyId.value === null)

  const selectedCompany = computed(() =>
    availableCompanies.value.find((c) => c.id === selectedCompanyId.value) ?? null)

  // Права текущего пользователя в выбранном хозяйстве (ТЗ §5). SystemAdmin — все права.
  const currentPermissions = computed<Set<string>>(() => {
    if (auth.isSystemAdmin) return new Set(ALL_PERMISSIONS)
    if (!selectedCompanyId.value) return new Set()
    const m = auth.memberships.find((x) => x.companyId === selectedCompanyId.value)
    return new Set((m?.permissions ?? []) as string[])
  })

  function has(permission: string): boolean {
    return currentPermissions.value.has(permission)
  }

  async function loadCompanies() {
    availableCompanies.value = await companiesApi.list()
    loaded.value = true

    // Если выбранное хозяйство более не доступно — сбрасываем на первое доступное.
    const ids = availableCompanies.value.map((c) => c.id)
    if (selectedCompanyId.value && !ids.includes(selectedCompanyId.value)) {
      selectCompany(availableCompanies.value[0]?.id ?? null)
    } else if (!selectedCompanyId.value && availableCompanies.value.length > 0) {
      // По умолчанию выбираем первое хозяйство (режим «Все хозяйства» — по кнопке).
      selectCompany(availableCompanies.value[0].id!)
    }
  }

  function selectCompany(companyId: string | null) {
    selectedCompanyId.value = companyId
    authStorage.setCompanyId(companyId)
  }

  function reset() {
    availableCompanies.value = []
    selectedCompanyId.value = null
    loaded.value = false
    authStorage.setCompanyId(null)
  }

  return {
    availableCompanies, selectedCompanyId, loaded, isAllCompaniesMode, selectedCompany,
    currentPermissions, has, loadCompanies, selectCompany, reset,
  }
})
