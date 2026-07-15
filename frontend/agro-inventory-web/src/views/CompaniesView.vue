<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useToast } from 'primevue/usetoast'
import { companiesApi } from '../api/companies'
import { adminUsersApi } from '../api/adminUsers'
import type { AdminUserDto, CompanyListItemDto, CreateCompanyRequest } from '../api/types'
import { AppRole, appRoleLabels } from '../api/types'
import { ApiError } from '../api/http'
import { useCompanyContextStore } from '../stores/companyContext'

const toast = useToast()
const ctx = useCompanyContextStore()

const companies = ref<CompanyListItemDto[]>([])
const users = ref<AdminUserDto[]>([])
const loading = ref(false)
const saving = ref(false)
const dialog = ref(false)
const editId = ref<string | null>(null)

const form = ref({
  name: '',
  legalName: '',
  binOrInn: '',
  country: 'Россия',
  timezone: 'Europe/Moscow',
  address: '',
  description: '',
  ownerUserId: null as string | null,
})

const userOptions = computed(() =>
  users.value.map((u) => ({
    label: `${u.firstName ?? ''} ${u.lastName ?? ''}`.trim() || u.email || 'Пользователь',
    value: u.id,
  })),
)

function fail(e: unknown, fallback: string) {
  toast.add({ severity: 'error', summary: 'Ошибка', detail: e instanceof ApiError ? e.message : fallback, life: 4500 })
}

async function load() {
  loading.value = true
  try {
    ;[companies.value, users.value] = await Promise.all([
      companiesApi.list(),
      adminUsersApi.list(),
    ])
  } catch (e) {
    fail(e, 'Не удалось загрузить хозяйства')
  } finally {
    loading.value = false
  }
}

function resetForm() {
  form.value = {
    name: '',
    legalName: '',
    binOrInn: '',
    country: 'Россия',
    timezone: 'Europe/Moscow',
    address: '',
    description: '',
    ownerUserId: null,
  }
}

function openCreate() {
  editId.value = null
  resetForm()
  dialog.value = true
}

async function openEdit(row: CompanyListItemDto) {
  if (!row.id) return
  try {
    const company = await companiesApi.get(row.id)
    editId.value = row.id
    form.value = {
      name: company.name ?? '',
      legalName: company.legalName ?? '',
      binOrInn: company.binOrInn ?? '',
      country: company.country ?? 'Россия',
      timezone: company.timezone ?? 'Europe/Moscow',
      address: company.address ?? '',
      description: company.description ?? '',
      ownerUserId: null,
    }
    dialog.value = true
  } catch (e) {
    fail(e, 'Не удалось открыть хозяйство')
  }
}

function body(): CreateCompanyRequest {
  return {
    name: form.value.name.trim(),
    legalName: form.value.legalName.trim() || null,
    binOrInn: form.value.binOrInn.trim() || null,
    country: form.value.country.trim() || 'Россия',
    timezone: form.value.timezone.trim() || 'Europe/Moscow',
    address: form.value.address.trim() || null,
    description: form.value.description.trim() || null,
  }
}

async function save() {
  if (!form.value.name.trim()) {
    toast.add({ severity: 'warn', summary: 'Укажите название хозяйства', life: 2500 })
    return
  }
  saving.value = true
  try {
    if (editId.value) {
      await companiesApi.update(editId.value, body())
      toast.add({ severity: 'success', summary: 'Хозяйство сохранено', life: 2200 })
    } else {
      const created = await companiesApi.create(body())
      if (created.id && form.value.ownerUserId) {
        await companiesApi.addMember(created.id, { userId: form.value.ownerUserId, role: AppRole.Owner })
      }
      toast.add({ severity: 'success', summary: 'Хозяйство создано', life: 2500 })
      await ctx.loadCompanies()
      if (created.id) ctx.selectCompany(created.id)
    }
    dialog.value = false
    await load()
  } catch (e) {
    fail(e, editId.value ? 'Не удалось сохранить хозяйство' : 'Не удалось создать хозяйство')
  } finally {
    saving.value = false
  }
}

onMounted(load)
</script>

<template>
  <section class="page">
    <div class="page__head">
      <h1 class="page__title">Хозяйства</h1>
      <PvButton label="Добавить хозяйство" icon="pi pi-plus" @click="openCreate" />
    </div>

    <PvDataTable :value="companies" :loading="loading" data-key="id" class="mt">
      <PvColumn field="name" header="Название" />
      <PvColumn header="Роль">
        <template #body="{ data }">
          <PvTag :value="data.isSystemAdmin ? 'Системный админ' : appRoleLabels[data.myRole]" severity="info" />
        </template>
      </PvColumn>
      <PvColumn header="" style="width: 8rem">
        <template #body="{ data }">
          <PvButton label="Открыть" size="small" text @click="openEdit(data)" />
        </template>
      </PvColumn>
      <template #empty><div class="empty">Хозяйств пока нет</div></template>
    </PvDataTable>

    <PvDialog v-model:visible="dialog" :header="editId ? 'Редактировать хозяйство' : 'Новое хозяйство'" modal :style="{ width: '34rem' }">
      <div class="form">
        <label class="field"><span>Название *</span><PvInputText v-model="form.name" /></label>
        <label class="field"><span>Юр. название</span><PvInputText v-model="form.legalName" /></label>
        <label class="field"><span>ИНН / БИН</span><PvInputText v-model="form.binOrInn" /></label>
        <div class="grid">
          <label class="field"><span>Страна</span><PvInputText v-model="form.country" /></label>
          <label class="field"><span>Часовой пояс</span><PvInputText v-model="form.timezone" /></label>
        </div>
        <label class="field"><span>Адрес</span><PvInputText v-model="form.address" /></label>
        <label class="field"><span>Описание</span><PvTextarea v-model="form.description" rows="2" auto-resize /></label>
        <label v-if="!editId" class="field"><span>Владелец</span>
          <PvSelect
            v-model="form.ownerUserId"
            :options="userOptions"
            option-label="label"
            option-value="value"
            filter
            show-clear
            placeholder="Можно назначить позже"
          />
        </label>
      </div>
      <template #footer>
        <PvButton label="Отмена" text @click="dialog = false" />
        <PvButton label="Сохранить" :loading="saving" @click="save" />
      </template>
    </PvDialog>
  </section>
</template>

<style scoped>
.page__head { display: flex; align-items: center; justify-content: space-between; gap: 1rem; }
.mt { margin-top: 1rem; }
.empty { padding: 1rem; color: #6b7280; }
.form { display: flex; flex-direction: column; gap: 0.75rem; }
.field { display: flex; flex-direction: column; gap: 0.25rem; }
.field > span { font-weight: 600; font-size: 0.9rem; }
.grid { display: grid; grid-template-columns: 1fr 1fr; gap: 0.75rem; }
@media (max-width: 640px) {
  .page__head { flex-direction: column; align-items: stretch; }
  .grid { grid-template-columns: 1fr; }
}
</style>
