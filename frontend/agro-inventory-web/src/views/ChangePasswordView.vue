<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useToast } from 'primevue/usetoast'
import { useAuthStore } from '../stores/auth'
import { useCompanyContextStore } from '../stores/companyContext'
import { ApiError } from '../api/http'

// Смена пароля (ТЗ §1). Используется и для обязательной смены временного пароля при первом входе.
const auth = useAuthStore()
const ctx = useCompanyContextStore()
const router = useRouter()
const toast = useToast()

const current = ref('')
const next = ref('')
const confirm = ref('')
const error = ref('')
const loading = ref(false)

async function submit() {
  error.value = ''
  if (next.value.length < 8) {
    error.value = 'Новый пароль должен быть не короче 8 символов.'
    return
  }
  if (next.value !== confirm.value) {
    error.value = 'Пароли не совпадают.'
    return
  }
  loading.value = true
  try {
    await auth.changePassword(current.value, next.value)
    toast.add({ severity: 'success', summary: 'Пароль изменён', life: 2500 })
    if (!ctx.loaded) await ctx.loadCompanies()
    router.replace('/')
  } catch (e) {
    error.value = e instanceof ApiError ? e.message : 'Не удалось сменить пароль.'
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="cp">
    <form class="cp__card" @submit.prevent="submit">
      <h1 class="cp__title">Смена пароля</h1>
      <p v-if="auth.mustChangePassword" class="cp__hint">
        Задан временный пароль. Придумайте новый пароль для продолжения работы.
      </p>

      <label class="cp__field">
        <span>Текущий пароль</span>
        <PvInputText v-model="current" type="password" autocomplete="current-password" :disabled="loading" />
      </label>
      <label class="cp__field">
        <span>Новый пароль</span>
        <PvInputText v-model="next" type="password" autocomplete="new-password" :disabled="loading" />
      </label>
      <label class="cp__field">
        <span>Повторите новый пароль</span>
        <PvInputText v-model="confirm" type="password" autocomplete="new-password" :disabled="loading" />
      </label>

      <PvMessage v-if="error" severity="error" :closable="false">{{ error }}</PvMessage>

      <PvButton type="submit" label="Сохранить" :loading="loading" class="cp__submit" />
    </form>
  </div>
</template>

<style scoped>
.cp { min-height: 100vh; display: flex; align-items: center; justify-content: center; padding: 1rem; }
.cp__card {
  width: 100%; max-width: 24rem; display: flex; flex-direction: column; gap: 0.9rem;
  padding: 2rem; border-radius: 14px;
  border: 1px solid var(--p-content-border-color, #e5e7eb);
  background: var(--p-content-background, #fff);
}
.cp__title { font-size: 1.4rem; font-weight: 700; margin: 0; }
.cp__hint { margin: 0; color: var(--p-text-muted-color, #6b7280); font-size: 0.9rem; }
.cp__field { display: flex; flex-direction: column; gap: 0.35rem; font-size: 0.9rem; }
.cp__field :deep(input) { width: 100%; }
.cp__submit { margin-top: 0.5rem; }
</style>
