<script setup lang="ts">
import { computed } from 'vue'
import { RouterView, RouterLink, useRouter, useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '../stores/auth'
import { useCompanyContextStore } from '../stores/companyContext'
import { Permissions } from '../api/types'
import CompanySwitcher from '../components/CompanySwitcher.vue'

const { t } = useI18n()
const auth = useAuthStore()
const ctx = useCompanyContextStore()
const router = useRouter()
const route = useRoute()

// Разделы, доступные в режиме «Все хозяйства» (ТЗ §15): агрегированная химия (§17) и глобальные
// админ-страницы, не зависящие от хозяйства. Прочие (приход/склады/дашборд и т.п.) требуют выбора
// конкретного хозяйства; общий дашборд и др. общие экраны — позже (этап G).
const allModeRoutes = computed(() => [
  'dashboard', 'chemicals', 'receipts', 'canonical-chemicals', 'companies', 'users', 'backups', 'settings',
  ...(auth.isSystemAdmin ? ['audit-log'] : []),
])
const showAllModePlaceholder = computed(() =>
  ctx.isAllCompaniesMode && !allModeRoutes.value.includes(route.name as string))

// Пункт навигации виден, если у пользователя есть право (или он SystemAdmin). ТЗ §5.
type NavItem = { to: string; key: string; icon?: string; perm?: string; systemAdmin?: boolean }

const primaryAll: NavItem[] = [
  { to: '/', key: 'nav.dashboard', icon: 'pi-home', perm: Permissions.ReportsView },
  { to: '/chemicals', key: 'nav.chemicals', icon: 'pi-box', perm: Permissions.InventoryView },
  { to: '/income', key: 'nav.income', icon: 'pi-plus-circle', perm: Permissions.ReceiptsCreate },
  { to: '/outcome', key: 'nav.outcome', icon: 'pi-minus-circle', perm: Permissions.WriteoffsCreate },
  { to: '/history', key: 'nav.history', icon: 'pi-clock', perm: Permissions.InventoryView },
]

const secondaryAll: NavItem[] = [
  { to: '/inventory-check', key: 'nav.inventoryCheck', perm: Permissions.InventoryView },
  { to: '/receipts', key: 'nav.receipts', perm: Permissions.ReceiptsView },
  { to: '/transfers', key: 'nav.transfers', perm: Permissions.TransfersView },
  { to: '/corrections', key: 'nav.corrections', perm: Permissions.AdjustmentsCreate },
  { to: '/crops', key: 'nav.crops', perm: Permissions.InventoryView },
  { to: '/warehouses', key: 'nav.warehouses', perm: Permissions.WarehousesView },
  { to: '/fields', key: 'nav.fields', perm: Permissions.FieldsView },
  { to: '/field-treatments', key: 'nav.fieldTreatments', perm: Permissions.TreatmentsView },
  { to: '/companies', key: 'nav.companies', systemAdmin: true },
  { to: '/members', key: 'nav.members', perm: Permissions.UsersView },
  { to: '/users', key: 'nav.users', systemAdmin: true },
  { to: '/canonical-chemicals', key: 'nav.canonicalChemicals', systemAdmin: true },
  { to: '/archive', key: 'nav.archive', perm: Permissions.InventoryView },
  { to: '/audit-log', key: 'nav.auditLog', perm: Permissions.AuditView },
  { to: '/backups', key: 'nav.backups', systemAdmin: true },
  { to: '/settings', key: 'nav.settings' },
]

function visible(i: NavItem) {
  if (i.systemAdmin) return auth.isSystemAdmin
  if (i.perm) return auth.isSystemAdmin || ctx.has(i.perm)
  return true
}

const primary = computed(() => primaryAll.filter(visible))
const secondary = computed(() => secondaryAll.filter(visible))

async function logout() {
  await auth.logout()
  ctx.reset()
  router.push({ name: 'login' })
}
</script>

<template>
  <div class="layout">
    <aside class="layout__sidebar">
      <div class="layout__brand">{{ t('app.name') }}</div>
      <nav class="layout__nav">
        <RouterLink v-for="i in primary" :key="i.to" :to="i.to" class="layout__link">
          <i class="pi" :class="i.icon" /> <span>{{ t(i.key) }}</span>
        </RouterLink>
        <div class="layout__divider" />
        <RouterLink v-for="i in secondary" :key="i.to" :to="i.to" class="layout__link">
          <span>{{ t(i.key) }}</span>
        </RouterLink>
      </nav>
    </aside>

    <div class="layout__main">
      <header class="topbar">
        <CompanySwitcher />
        <div class="topbar__spacer" />
        <div class="topbar__user">
          <span class="topbar__name">{{ auth.displayName }}</span>
          <RouterLink to="/change-password" class="topbar__action" title="Сменить пароль">
            <i class="pi pi-key" />
          </RouterLink>
          <button class="topbar__action" title="Выйти" @click="logout"><i class="pi pi-sign-out" /></button>
        </div>
      </header>

      <main class="layout__content">
        <div v-if="showAllModePlaceholder" class="allmode">
          <i class="pi pi-building allmode__icon" />
          <p>Этот раздел доступен в контексте конкретного хозяйства.</p>
          <p class="allmode__muted">Выберите хозяйство в переключателе сверху.</p>
        </div>
        <RouterView v-else :key="ctx.selectedCompanyId ?? 'all'" />
      </main>
    </div>

    <!-- Нижняя навигация для телефонов -->
    <nav class="layout__bottombar">
      <RouterLink v-for="i in primary" :key="i.to" :to="i.to" class="layout__tab">
        <i class="pi" :class="i.icon" />
        <span>{{ t(i.key) }}</span>
      </RouterLink>
    </nav>
  </div>
</template>

<style scoped>
.layout {
  display: grid;
  grid-template-columns: 240px 1fr;
  min-height: 100vh;
}
.layout__sidebar {
  border-right: 1px solid var(--p-content-border-color, #e5e7eb);
  padding: 1rem;
  position: sticky;
  top: 0;
  height: 100vh;
  overflow-y: auto;
}
.layout__brand {
  font-weight: 700;
  font-size: 1.25rem;
  margin-bottom: 1rem;
}
.layout__nav { display: flex; flex-direction: column; gap: 0.25rem; }
.layout__link {
  display: flex; align-items: center; gap: 0.5rem;
  padding: 0.5rem 0.75rem; border-radius: 8px; text-decoration: none;
  color: inherit;
}
.layout__link.router-link-exact-active { background: rgba(59, 130, 246, 0.12); font-weight: 600; }
.layout__divider { height: 1px; background: var(--p-content-border-color, #e5e7eb); margin: 0.5rem 0; }
.layout__main { display: flex; flex-direction: column; min-width: 0; }
.topbar {
  display: flex; align-items: center; gap: 0.75rem;
  padding: 0.6rem 1.5rem;
  border-bottom: 1px solid var(--p-content-border-color, #e5e7eb);
  position: sticky; top: 0; z-index: 5;
  background: var(--p-content-background, #fff);
}
.topbar__spacer { flex: 1; }
.topbar__user { display: flex; align-items: center; gap: 0.5rem; }
.topbar__name { font-size: 0.9rem; color: var(--p-text-muted-color, #6b7280); }
.topbar__action {
  display: inline-flex; align-items: center; justify-content: center;
  width: 2rem; height: 2rem; border-radius: 8px;
  border: none; background: transparent; color: inherit; cursor: pointer; text-decoration: none;
}
.topbar__action:hover { background: rgba(0,0,0,0.06); }
.layout__content { padding: 1.5rem; }
.allmode { display: flex; flex-direction: column; align-items: center; gap: 0.4rem; padding: 4rem 1rem; text-align: center; }
.allmode__icon { font-size: 2rem; color: var(--p-text-muted-color, #9ca3af); margin-bottom: 0.5rem; }
.allmode__muted { color: var(--p-text-muted-color, #6b7280); font-size: 0.9rem; }
.layout__bottombar { display: none; }

@media (max-width: 768px) {
  .layout { grid-template-columns: 1fr; }
  .layout__sidebar { display: none; }
  .topbar { padding: 0.6rem 1rem; }
  .layout__content { padding: 1rem 1rem calc(5.5rem + env(safe-area-inset-bottom)); }
  .layout__bottombar {
    display: flex; justify-content: space-around;
    position: fixed; bottom: 0; left: 0; right: 0;
    background: var(--p-content-background, #fff);
    border-top: 1px solid var(--p-content-border-color, #e5e7eb);
    padding: 0.5rem 0 calc(0.5rem + env(safe-area-inset-bottom));
    z-index: 10;
  }
  .layout__tab {
    display: flex; flex-direction: column; align-items: center; gap: 3px;
    font-size: 0.7rem; text-decoration: none; color: inherit;
    padding: 0.35rem 0.5rem; min-width: 56px; border-radius: 10px;
  }
  .layout__tab i { font-size: 1.15rem; }
  .layout__tab.router-link-exact-active { color: #16a34a; }
}
</style>
