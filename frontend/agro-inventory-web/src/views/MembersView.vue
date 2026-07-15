<script setup lang="ts">
import { onMounted, ref, computed } from 'vue'
import { useToast } from 'primevue/usetoast'
import { companiesApi } from '../api/companies'
import { adminUsersApi } from '../api/adminUsers'
import { warehousesApi, fieldsApi } from '../api/reference'
import { useAuthStore } from '../stores/auth'
import { useCompanyContextStore } from '../stores/companyContext'
import {
  companyRoleOptions, appRoleLabels, membershipStatusLabels, MembershipStatus, AccessScopeType,
} from '../api/types'
import type { MemberDto, AdminUserDto, WarehouseDto, FieldDto, ScopeItemDto } from '../api/types'
import { ApiError } from '../api/http'

// Члены хозяйства и области доступа (ТЗ §3, §6, §21). Работает в контексте выбранного хозяйства.
const toast = useToast()
const auth = useAuthStore()
const ctx = useCompanyContextStore()

const companyId = computed(() => ctx.selectedCompanyId)
const members = ref<MemberDto[]>([])
const loading = ref(false)

const addDialog = ref(false)
const allUsers = ref<AdminUserDto[]>([])
const addForm = ref({ userId: '', role: 3 })

const scopeDialog = ref(false)
const scopeMember = ref<MemberDto | null>(null)
const fullScope = ref(true)
const warehouses = ref<WarehouseDto[]>([])
const fields = ref<FieldDto[]>([])
const selectedWarehouses = ref<string[]>([])
const selectedFields = ref<string[]>([])

const memberStatusOptions = [MembershipStatus.Active, MembershipStatus.Suspended]
  .map((v) => ({ value: v, label: membershipStatusLabels[v] }))

function fail(e: unknown, fallback: string) {
  toast.add({ severity: 'error', summary: 'Ошибка', detail: e instanceof ApiError ? e.message : fallback, life: 4000 })
}

async function load() {
  if (!companyId.value) return
  loading.value = true
  try {
    members.value = await companiesApi.members(companyId.value)
  } catch (e) {
    fail(e, 'Не удалось загрузить состав хозяйства')
  } finally {
    loading.value = false
  }
}

async function openAdd() {
  addForm.value = { userId: '', role: 3 }
  try {
    // Список аккаунтов доступен системному администратору (ТЗ §21).
    allUsers.value = await adminUsersApi.list()
  } catch {
    allUsers.value = []
  }
  addDialog.value = true
}

const availableUserOptions = computed(() => {
  const memberIds = new Set(members.value.map((m) => m.userId))
  return allUsers.value
    .filter((u) => !memberIds.has(u.id))
    .map((u) => ({ value: u.id, label: `${u.firstName} ${u.lastName} (${u.email})` }))
})

async function addMember() {
  if (!companyId.value || !addForm.value.userId) return
  try {
    await companiesApi.addMember(companyId.value,
      { userId: addForm.value.userId, role: addForm.value.role as MemberDto['role'] })
    addDialog.value = false
    toast.add({ severity: 'success', summary: 'Пользователь добавлен', life: 2500 })
    await load()
  } catch (e) {
    fail(e, 'Не удалось добавить пользователя')
  }
}

async function changeRole(m: MemberDto, role: number) {
  if (!companyId.value) return
  try {
    await companiesApi.updateMember(companyId.value, m.membershipId!,
      { role: role as MemberDto['role'], status: m.status! })
    await load()
  } catch (e) {
    fail(e, 'Не удалось изменить роль')
    await load()
  }
}

async function changeStatus(m: MemberDto, status: number) {
  if (!companyId.value) return
  try {
    await companiesApi.updateMember(companyId.value, m.membershipId!,
      { role: m.role!, status: status as MemberDto['status'] })
    await load()
  } catch (e) {
    fail(e, 'Не удалось изменить статус')
    await load()
  }
}

async function removeMember(m: MemberDto) {
  if (!companyId.value) return
  try {
    await companiesApi.removeMember(companyId.value, m.membershipId!)
    toast.add({ severity: 'success', summary: 'Доступ отключён', life: 2000 })
    await load()
  } catch (e) {
    fail(e, 'Не удалось удалить из хозяйства')
  }
}

async function openScopes(m: MemberDto) {
  if (!companyId.value) return
  scopeMember.value = m
  try {
    const [scopes, ws, fs] = await Promise.all([
      companiesApi.getScopes(companyId.value, m.membershipId!),
      warehousesApi.list(),
      fieldsApi.list(),
    ])
    warehouses.value = ws
    fields.value = fs
    fullScope.value = scopes.hasFullCompanyScope ?? true
    const items = (scopes.scopes ?? []) as ScopeItemDto[]
    selectedWarehouses.value = items.filter((s) => s.scopeType === AccessScopeType.Warehouse)
      .map((s) => s.scopeEntityId!).filter(Boolean)
    selectedFields.value = items.filter((s) => s.scopeType === AccessScopeType.Field)
      .map((s) => s.scopeEntityId!).filter(Boolean)
    scopeDialog.value = true
  } catch (e) {
    fail(e, 'Не удалось загрузить области доступа')
  }
}

async function saveScopes() {
  if (!companyId.value || !scopeMember.value) return
  const scopes: ScopeItemDto[] = fullScope.value
    ? [{ scopeType: AccessScopeType.Company, scopeEntityId: null }]
    : [
        ...selectedWarehouses.value.map((id) => ({ scopeType: AccessScopeType.Warehouse, scopeEntityId: id })),
        ...selectedFields.value.map((id) => ({ scopeType: AccessScopeType.Field, scopeEntityId: id })),
      ]
  try {
    await companiesApi.updateScopes(companyId.value, scopeMember.value.membershipId!, { scopes })
    scopeDialog.value = false
    toast.add({ severity: 'success', summary: 'Доступ сохранён', life: 2000 })
  } catch (e) {
    fail(e, 'Не удалось сохранить области доступа')
  }
}

onMounted(load)
</script>

<template>
  <section class="page">
    <div class="page__head">
      <h1 class="page__title">Состав хозяйства</h1>
      <PvButton v-if="auth.isSystemAdmin" label="Добавить" icon="pi pi-plus" @click="openAdd" />
    </div>

    <PvDataTable :value="members" :loading="loading" data-key="membershipId" class="mt">
      <PvColumn header="Пользователь">
        <template #body="{ data }">{{ data.displayName }}<br /><small>{{ data.email }}</small></template>
      </PvColumn>
      <PvColumn header="Роль" style="width: 16rem">
        <template #body="{ data }">
          <PvSelect :model-value="data.role" :options="companyRoleOptions"
                    option-label="label" option-value="value"
                    @update:model-value="(v: number) => changeRole(data, v)" />
        </template>
      </PvColumn>
      <PvColumn header="Статус" style="width: 12rem">
        <template #body="{ data }">
          <PvSelect v-if="data.status !== MembershipStatus.Removed"
                    :model-value="data.status" :options="memberStatusOptions"
                    option-label="label" option-value="value"
                    @update:model-value="(v: number) => changeStatus(data, v)" />
          <PvTag v-else :value="membershipStatusLabels[data.status]" severity="danger" />
        </template>
      </PvColumn>
      <PvColumn header="" style="width: 14rem">
        <template #body="{ data }">
          <PvButton size="small" text label="Доступ" icon="pi pi-key" @click="openScopes(data)" />
          <PvButton size="small" text label="Убрать" severity="danger" @click="removeMember(data)" />
        </template>
      </PvColumn>
    </PvDataTable>

    <!-- Добавление члена -->
    <PvDialog v-model:visible="addDialog" header="Добавить в хозяйство" modal :style="{ width: '30rem' }">
      <div class="form">
        <label class="form__field">
          <span>Пользователь</span>
          <PvSelect v-model="addForm.userId" :options="availableUserOptions"
                    option-label="label" option-value="value" filter placeholder="Выберите аккаунт" />
        </label>
        <label class="form__field">
          <span>Роль</span>
          <PvSelect v-model="addForm.role" :options="companyRoleOptions" option-label="label" option-value="value" />
        </label>
        <p class="form__hint">Новый член по умолчанию получает доступ ко всему хозяйству — сузить можно через «Доступ».</p>
      </div>
      <template #footer>
        <PvButton label="Отмена" text @click="addDialog = false" />
        <PvButton label="Добавить" :disabled="!addForm.userId" @click="addMember" />
      </template>
    </PvDialog>

    <!-- Области доступа -->
    <PvDialog v-model:visible="scopeDialog" header="Область доступа" modal :style="{ width: '32rem' }">
      <p class="form__hint" v-if="scopeMember">
        {{ scopeMember.displayName }} — {{ appRoleLabels[scopeMember.role!] }}
      </p>
      <label class="form__row"><PvToggleSwitch v-model="fullScope" /><span>Доступ ко всему хозяйству</span></label>

      <template v-if="!fullScope">
        <label class="form__field">
          <span>Склады</span>
          <PvMultiSelect v-model="selectedWarehouses" :options="warehouses" option-label="number" option-value="id"
                         placeholder="Выберите склады" filter display="chip" />
        </label>
        <label class="form__field">
          <span>Поля</span>
          <PvMultiSelect v-model="selectedFields" :options="fields" option-label="number" option-value="id"
                         placeholder="Выберите поля" filter display="chip" />
        </label>
        <p class="form__hint">Без склада/поля пользователь не увидит соответствующих данных (ТЗ §6).</p>
      </template>

      <template #footer>
        <PvButton label="Отмена" text @click="scopeDialog = false" />
        <PvButton label="Сохранить" @click="saveScopes" />
      </template>
    </PvDialog>
  </section>
</template>

<style scoped>
.page__head { display: flex; justify-content: space-between; align-items: center; }
.mt { margin-top: 1rem; }
.form { display: flex; flex-direction: column; gap: 0.75rem; }
.form__field { display: flex; flex-direction: column; gap: 0.3rem; font-size: 0.9rem; }
.form__row { display: flex; align-items: center; gap: 0.6rem; margin: 0.5rem 0; }
.form__hint { color: var(--p-text-muted-color, #6b7280); font-size: 0.85rem; margin: 0.25rem 0; }
</style>
