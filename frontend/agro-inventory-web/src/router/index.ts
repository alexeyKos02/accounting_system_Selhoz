import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router'
import { Permissions } from '../api/types'
import { useAuthStore } from '../stores/auth'
import { useCompanyContextStore } from '../stores/companyContext'
import { authStorage } from '../api/authStorage'

// Роуты (ТЗ §28). Аутентификация/мультиарендность (ТЗ §1, §15): экраны входа и смены пароля —
// вне общего layout; остальные страницы — дети AppLayout (шапка с переключателем хозяйств).
const routes: RouteRecordRaw[] = [
  { path: '/login', name: 'login', component: () => import('../views/LoginView.vue'), meta: { public: true } },
  { path: '/change-password', name: 'change-password', component: () => import('../views/ChangePasswordView.vue'), meta: { noCompany: true } },
  {
    path: '/',
    component: () => import('../layouts/AppLayout.vue'),
    children: [
      { path: '', name: 'dashboard', component: () => import('../views/DashboardView.vue') },
      { path: 'chemicals', name: 'chemicals', component: () => import('../views/ChemicalsView.vue') },
      { path: 'chemicals/create', name: 'chemical-create', component: () => import('../views/ChemicalCreateView.vue') },
      { path: 'chemicals/:id', name: 'chemical-detail', component: () => import('../views/ChemicalDetailView.vue'), props: true },
      { path: 'income', name: 'income', component: () => import('../views/IncomeView.vue') },
      { path: 'income/bulk', name: 'income-bulk', component: () => import('../views/BulkIncomeView.vue') },
      { path: 'outcome', name: 'outcome', component: () => import('../views/OutcomeView.vue') },
      { path: 'corrections', name: 'corrections', component: () => import('../views/CorrectionsView.vue') },
      { path: 'inventory-check', name: 'inventory-check', component: () => import('../views/InventoryCheckView.vue') },
      { path: 'history', name: 'history', component: () => import('../views/HistoryView.vue') },
      { path: 'audit-log', name: 'audit-log', component: () => import('../views/AuditLogView.vue'), meta: { systemAdmin: true } },
      { path: 'crops', name: 'crops', component: () => import('../views/CropsView.vue') },
      { path: 'warehouses', name: 'warehouses', component: () => import('../views/WarehousesView.vue') },
      { path: 'fields', name: 'fields', component: () => import('../views/FieldsView.vue') },
      { path: 'members', name: 'members', component: () => import('../views/MembersView.vue'), meta: { permission: Permissions.UsersView } },
      { path: 'users', name: 'users', component: () => import('../views/UsersView.vue'), meta: { systemAdmin: true } },
      { path: 'canonical-chemicals', name: 'canonical-chemicals', component: () => import('../views/CanonicalChemicalsView.vue'), meta: { systemAdmin: true } },
      { path: 'archive', name: 'archive', component: () => import('../views/ArchiveView.vue') },
      { path: 'backups', name: 'backups', component: () => import('../views/BackupsView.vue'), meta: { systemAdmin: true } },
      { path: 'settings', name: 'settings', component: () => import('../views/SettingsView.vue') },
      { path: 'spare-parts', name: 'spare-parts', component: () => import('../views/SparePartsView.vue') },
    ],
  },
  { path: '/:pathMatch(.*)*', name: 'not-found', component: () => import('../views/NotFoundView.vue') },
]

export const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes,
  scrollBehavior: () => ({ top: 0 }),
})

// Гвард: аутентификация, обязательная смена пароля, доступ по правам (ТЗ §1, §5, §24).
router.beforeEach(async (to) => {
  const auth = useAuthStore()
  const ctx = useCompanyContextStore()

  if (to.meta.public) {
    // На страницу входа не пускаем уже вошедших.
    if (auth.hasToken && auth.user && !auth.mustChangePassword) return { path: '/' }
    return true
  }

  // Нет токена — на вход.
  if (!authStorage.getAccess()) {
    return { name: 'login', query: to.fullPath !== '/' ? { redirect: to.fullPath } : {} }
  }

  // Подгружаем профиль и хозяйства при первом заходе (после перезагрузки страницы).
  if (!auth.user) {
    try {
      await auth.fetchMe()
    } catch {
      auth.reset()
      return { name: 'login' }
    }
  }

  // Обязательная смена временного пароля (ТЗ §1).
  if (auth.mustChangePassword && to.name !== 'change-password') {
    return { name: 'change-password' }
  }
  if (to.meta.noCompany) return true

  if (!ctx.loaded) {
    try {
      await ctx.loadCompanies()
    } catch {
      /* список хозяйств подтянется позже */
    }
  }

  // Доступ по правам роли в выбранном хозяйстве (ТЗ §5). Итоговая проверка — на backend.
  if (to.meta.systemAdmin && !auth.isSystemAdmin) return { path: '/' }
  if (typeof to.meta.permission === 'string' && !ctx.has(to.meta.permission) && !auth.isSystemAdmin) {
    return { path: '/' }
  }

  return true
})

export default router
