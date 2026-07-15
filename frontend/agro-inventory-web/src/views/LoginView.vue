<script setup lang="ts">
import { ref } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import { useCompanyContextStore } from '../stores/companyContext'
import { ApiError, NetworkError } from '../api/http'

const auth = useAuthStore()
const ctx = useCompanyContextStore()
const router = useRouter()
const route = useRoute()

const email = ref('')
const password = ref('')
const error = ref('')
const loading = ref(false)

async function submit() {
  error.value = ''
  if (!email.value || !password.value) {
    error.value = 'Введите e-mail и пароль.'
    return
  }
  loading.value = true
  try {
    await auth.login({ email: email.value.trim(), password: password.value })
    if (auth.mustChangePassword) {
      router.replace({ name: 'change-password' })
      return
    }
    await ctx.loadCompanies()
    const redirect = (route.query.redirect as string) || '/'
    router.replace(redirect)
  } catch (e) {
    error.value =
      e instanceof ApiError ? e.message
      : e instanceof NetworkError ? 'Нет соединения с сервером.'
      : 'Не удалось войти.'
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="login">
    <form class="login__card" @submit.prevent="submit">
      <h1 class="login__title">AgroInventory</h1>
      <p class="login__subtitle">Вход в систему</p>

      <label class="login__field">
        <span>E-mail</span>
        <PvInputText v-model="email" type="email" autocomplete="username" :disabled="loading" />
      </label>

      <label class="login__field">
        <span>Пароль</span>
        <PvInputText v-model="password" type="password" autocomplete="current-password" :disabled="loading" />
      </label>

      <PvMessage v-if="error" severity="error" :closable="false">{{ error }}</PvMessage>

      <PvButton type="submit" label="Войти" :loading="loading" class="login__submit" />
    </form>
  </div>
</template>

<style scoped>
.login { min-height: 100vh; display: flex; align-items: center; justify-content: center; padding: 1rem; }
.login__card {
  width: 100%; max-width: 22rem; display: flex; flex-direction: column; gap: 0.9rem;
  padding: 2rem; border-radius: 14px;
  border: 1px solid var(--p-content-border-color, #e5e7eb);
  background: var(--p-content-background, #fff);
}
.login__title { font-size: 1.5rem; font-weight: 700; margin: 0; }
.login__subtitle { margin: 0 0 0.5rem; color: var(--p-text-muted-color, #6b7280); }
.login__field { display: flex; flex-direction: column; gap: 0.35rem; font-size: 0.9rem; }
.login__field :deep(input) { width: 100%; }
.login__submit { margin-top: 0.5rem; }
</style>
