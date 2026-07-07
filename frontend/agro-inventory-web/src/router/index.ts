import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router'

// Роуты соответствуют ТЗ §28. Views пока заглушки — наполняются по этапам.
const routes: RouteRecordRaw[] = [
  { path: '/', name: 'dashboard', component: () => import('../views/DashboardView.vue') },
  { path: '/chemicals', name: 'chemicals', component: () => import('../views/ChemicalsView.vue') },
  { path: '/chemicals/create', name: 'chemical-create', component: () => import('../views/ChemicalCreateView.vue') },
  { path: '/chemicals/:id', name: 'chemical-detail', component: () => import('../views/ChemicalDetailView.vue'), props: true },
  { path: '/income', name: 'income', component: () => import('../views/IncomeView.vue') },
  { path: '/outcome', name: 'outcome', component: () => import('../views/OutcomeView.vue') },
  { path: '/corrections', name: 'corrections', component: () => import('../views/CorrectionsView.vue') },
  { path: '/inventory-check', name: 'inventory-check', component: () => import('../views/InventoryCheckView.vue') },
  { path: '/history', name: 'history', component: () => import('../views/HistoryView.vue') },
  { path: '/audit-log', name: 'audit-log', component: () => import('../views/AuditLogView.vue') },
  { path: '/crops', name: 'crops', component: () => import('../views/CropsView.vue') },
  { path: '/warehouses', name: 'warehouses', component: () => import('../views/WarehousesView.vue') },
  { path: '/archive', name: 'archive', component: () => import('../views/ArchiveView.vue') },
  { path: '/backups', name: 'backups', component: () => import('../views/BackupsView.vue') },
  { path: '/settings', name: 'settings', component: () => import('../views/SettingsView.vue') },
  { path: '/spare-parts', name: 'spare-parts', component: () => import('../views/SparePartsView.vue') },
  { path: '/:pathMatch(.*)*', name: 'not-found', component: () => import('../views/NotFoundView.vue') },
]

export const router = createRouter({
  // BASE_URL учитывает подпуть GitHub Pages (см. vite.config.ts).
  history: createWebHistory(import.meta.env.BASE_URL),
  routes,
  scrollBehavior: () => ({ top: 0 }),
})

export default router
