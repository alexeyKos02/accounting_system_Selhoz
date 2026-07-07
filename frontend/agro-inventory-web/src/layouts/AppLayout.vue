<script setup lang="ts">
import { RouterView, RouterLink } from 'vue-router'
import { useI18n } from 'vue-i18n'

const { t } = useI18n()

// Основная навигация. mobile/tablet-first: на узких экранах — нижняя панель,
// на широких — боковое меню (ТЗ §5, §31.10).
const primary = [
  { to: '/', key: 'nav.dashboard', icon: 'pi-home' },
  { to: '/chemicals', key: 'nav.chemicals', icon: 'pi-box' },
  { to: '/income', key: 'nav.income', icon: 'pi-plus-circle' },
  { to: '/outcome', key: 'nav.outcome', icon: 'pi-minus-circle' },
  { to: '/history', key: 'nav.history', icon: 'pi-clock' },
]

const secondary = [
  { to: '/inventory-check', key: 'nav.inventoryCheck' },
  { to: '/corrections', key: 'nav.corrections' },
  { to: '/crops', key: 'nav.crops' },
  { to: '/warehouses', key: 'nav.warehouses' },
  { to: '/archive', key: 'nav.archive' },
  { to: '/audit-log', key: 'nav.auditLog' },
  { to: '/backups', key: 'nav.backups' },
  { to: '/settings', key: 'nav.settings' },
  { to: '/spare-parts', key: 'nav.spareParts' },
]
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

    <main class="layout__content">
      <RouterView />
    </main>

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
.layout__content { padding: 1.5rem; }
.layout__bottombar { display: none; }

@media (max-width: 768px) {
  .layout { grid-template-columns: 1fr; }
  .layout__sidebar { display: none; }
  .layout__content { padding: 1rem 1rem 5rem; }
  .layout__bottombar {
    display: flex; justify-content: space-around;
    position: fixed; bottom: 0; left: 0; right: 0;
    background: var(--p-content-background, #fff);
    border-top: 1px solid var(--p-content-border-color, #e5e7eb);
    padding: 0.4rem 0; z-index: 10;
  }
  .layout__tab {
    display: flex; flex-direction: column; align-items: center; gap: 2px;
    font-size: 0.7rem; text-decoration: none; color: inherit; padding: 0.2rem 0.5rem;
  }
  .layout__tab.router-link-exact-active { color: #3b82f6; }
}
</style>
