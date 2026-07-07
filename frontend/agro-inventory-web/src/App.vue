<script setup lang="ts">
import { onMounted, ref } from 'vue'
import AppLayout from './layouts/AppLayout.vue'
import RecoveryScreen from './views/RecoveryScreen.vue'
import { checkDatabase } from './api/health'

// При старте проверяем доступность БД. Если недоступна/без схемы — аварийный экран (ТЗ §24.6).
type AppState = 'loading' | 'ready' | 'recovery'
const state = ref<AppState>('loading')

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

  <AppLayout v-else />
</template>

<style scoped>
.boot { min-height: 100vh; display: flex; align-items: center; justify-content: center; }
</style>
