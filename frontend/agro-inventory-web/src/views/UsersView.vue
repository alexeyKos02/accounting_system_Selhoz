<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useToast } from 'primevue/usetoast'
import { adminUsersApi } from '../api/adminUsers'
import type { AdminUserDto } from '../api/types'
import { userStatusLabels, UserStatus } from '../api/types'
import { ApiError } from '../api/http'

// Управление аккаунтами (ТЗ §1, §21). Доступно только системному администратору.
const toast = useToast()
const users = ref<AdminUserDto[]>([])
const loading = ref(false)

const createDialog = ref(false)
const form = ref({ email: '', password: '', firstName: '', lastName: '', phone: '', isSystemAdmin: false })
const saving = ref(false)

const resetDialog = ref(false)
const resetUser = ref<AdminUserDto | null>(null)
const resetPassword = ref('')

function fail(e: unknown, fallback: string) {
  toast.add({ severity: 'error', summary: 'Ошибка', detail: e instanceof ApiError ? e.message : fallback, life: 4000 })
}

async function load() {
  loading.value = true
  try {
    users.value = await adminUsersApi.list()
  } catch (e) {
    fail(e, 'Не удалось загрузить пользователей')
  } finally {
    loading.value = false
  }
}

function openCreate() {
  form.value = { email: '', password: '', firstName: '', lastName: '', phone: '', isSystemAdmin: false }
  createDialog.value = true
}

async function create() {
  saving.value = true
  try {
    await adminUsersApi.create({
      email: form.value.email.trim(),
      password: form.value.password,
      firstName: form.value.firstName.trim(),
      lastName: form.value.lastName.trim(),
      phone: form.value.phone.trim() || null,
      isSystemAdmin: form.value.isSystemAdmin,
    })
    createDialog.value = false
    toast.add({ severity: 'success', summary: 'Пользователь создан', life: 2500 })
    await load()
  } catch (e) {
    fail(e, 'Не удалось создать пользователя')
  } finally {
    saving.value = false
  }
}

async function toggleBlock(u: AdminUserDto) {
  try {
    await adminUsersApi.block(u.id!, u.status !== UserStatus.Blocked)
    await load()
  } catch (e) {
    fail(e, 'Не удалось изменить статус')
  }
}

function openReset(u: AdminUserDto) {
  resetUser.value = u
  resetPassword.value = ''
  resetDialog.value = true
}

async function doReset() {
  if (!resetUser.value) return
  try {
    await adminUsersApi.resetPassword(resetUser.value.id!, resetPassword.value)
    resetDialog.value = false
    toast.add({ severity: 'success', summary: 'Пароль сброшен', detail: 'Пользователь сменит его при входе.', life: 3500 })
  } catch (e) {
    fail(e, 'Не удалось сбросить пароль')
  }
}

onMounted(load)
</script>

<template>
  <section class="page">
    <div class="page__head">
      <h1 class="page__title">Пользователи</h1>
      <PvButton label="Создать" icon="pi pi-plus" @click="openCreate" />
    </div>

    <PvDataTable :value="users" :loading="loading" data-key="id" class="mt">
      <PvColumn field="email" header="E-mail" />
      <PvColumn header="Имя">
        <template #body="{ data }">{{ data.firstName }} {{ data.lastName }}</template>
      </PvColumn>
      <PvColumn field="phone" header="Телефон" />
      <PvColumn header="Роль">
        <template #body="{ data }">
          <PvTag v-if="data.isSystemAdmin" value="Системный админ" severity="warn" />
          <span v-else>—</span>
        </template>
      </PvColumn>
      <PvColumn header="Статус">
        <template #body="{ data }">
          <PvTag :value="userStatusLabels[data.status]"
                 :severity="data.status === UserStatus.Active ? 'success' : 'danger'" />
          <PvTag v-if="data.mustChangePassword" value="сменит пароль" severity="info" class="ml" />
        </template>
      </PvColumn>
      <PvColumn header="" style="width: 16rem">
        <template #body="{ data }">
          <PvButton size="small" text label="Сбросить пароль" @click="openReset(data)" />
          <PvButton size="small" text
                    :label="data.status === UserStatus.Blocked ? 'Разблокировать' : 'Заблокировать'"
                    :severity="data.status === UserStatus.Blocked ? 'success' : 'danger'"
                    @click="toggleBlock(data)" />
        </template>
      </PvColumn>
    </PvDataTable>

    <PvDialog v-model:visible="createDialog" header="Новый пользователь" modal :style="{ width: '28rem' }">
      <div class="form">
        <label class="form__field"><span>E-mail</span><PvInputText v-model="form.email" /></label>
        <label class="form__field"><span>Временный пароль</span><PvInputText v-model="form.password" /></label>
        <label class="form__field"><span>Имя</span><PvInputText v-model="form.firstName" /></label>
        <label class="form__field"><span>Фамилия</span><PvInputText v-model="form.lastName" /></label>
        <label class="form__field"><span>Телефон</span><PvInputText v-model="form.phone" /></label>
        <label class="form__row">
          <PvToggleSwitch v-model="form.isSystemAdmin" /><span>Системный администратор</span>
        </label>
        <p class="form__hint">Пользователь обязан сменить временный пароль при первом входе.</p>
      </div>
      <template #footer>
        <PvButton label="Отмена" text @click="createDialog = false" />
        <PvButton label="Создать" :loading="saving" @click="create" />
      </template>
    </PvDialog>

    <PvDialog v-model:visible="resetDialog" header="Сброс пароля" modal :style="{ width: '24rem' }">
      <p class="form__hint">Новый временный пароль для {{ resetUser?.email }}. Пользователь сменит его при входе.</p>
      <PvInputText v-model="resetPassword" class="w-full" placeholder="Новый временный пароль" />
      <template #footer>
        <PvButton label="Отмена" text @click="resetDialog = false" />
        <PvButton label="Сбросить" @click="doReset" />
      </template>
    </PvDialog>
  </section>
</template>

<style scoped>
.page__head { display: flex; justify-content: space-between; align-items: center; }
.mt { margin-top: 1rem; }
.ml { margin-left: 0.35rem; }
.w-full { width: 100%; }
.form { display: flex; flex-direction: column; gap: 0.75rem; }
.form__field { display: flex; flex-direction: column; gap: 0.3rem; font-size: 0.9rem; }
.form__field :deep(input) { width: 100%; }
.form__row { display: flex; align-items: center; gap: 0.6rem; }
.form__hint { color: var(--p-text-muted-color, #6b7280); font-size: 0.85rem; margin: 0; }
</style>
