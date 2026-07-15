<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { RouterView, useRouter } from 'vue-router'
import RecoveryScreen from './views/RecoveryScreen.vue'
import { checkDatabase } from './api/health'
import { setUnauthorizedHandler } from './api/http'
import { useAuthStore } from './stores/auth'
import { useCompanyContextStore } from './stores/companyContext'

// При старте проверяем доступность БД. Если недоступна/без схемы — аварийный экран (ТЗ §24.6).
type AppState = 'loading' | 'ready' | 'recovery'
const state = ref<AppState>('loading')
const router = useRouter()
const auth = useAuthStore()
const ctx = useCompanyContextStore()

// Окончательная потеря авторизации (refresh не удался) — очистка и переход на вход (ТЗ §1).
setUnauthorizedHandler(() => {
  auth.reset()
  ctx.reset()
  if (router.currentRoute.value.name !== 'login') router.push({ name: 'login' })
})

async function check() {
  state.value = 'loading'
  const health = await checkDatabase()
  state.value = health.canConnect && health.schemaReady ? 'ready' : 'recovery'
}

onMounted(check)
</script>

<template>
  <PvToast position="top-right" />

  <div v-if="state === 'loading'" class="boot">
    <PvProgressSpinner />
  </div>

  <RecoveryScreen v-else-if="state === 'recovery'" @recheck="check" />

  <RouterView v-else />
</template>

<style scoped>
.boot { min-height: 100vh; display: flex; align-items: center; justify-content: center; }
</style>
