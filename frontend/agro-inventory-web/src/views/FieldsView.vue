<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useToast } from 'primevue/usetoast'
import { fieldsApi } from '../api/reference'
import type { FieldDto } from '../api/types'
import { ApiError } from '../api/http'

const toast = useToast()
const fields = ref<FieldDto[]>([])
const loading = ref(false)
const newNumber = ref('')
const saving = ref(false)

const editDialog = ref(false)
const editId = ref<string | null>(null)
const editNumber = ref('')

async function load() {
  loading.value = true
  try {
    fields.value = await fieldsApi.list()
  } finally {
    loading.value = false
  }
}

function fail(e: unknown, fallback: string) {
  const msg = e instanceof ApiError ? e.message : fallback
  toast.add({ severity: 'error', summary: 'Ошибка', detail: msg, life: 4000 })
}

async function add() {
  const number = newNumber.value.trim()
  if (!number) return
  saving.value = true
  try {
    await fieldsApi.create(number)
    newNumber.value = ''
    toast.add({ severity: 'success', summary: 'Поле добавлено', life: 2000 })
    await load()
  } catch (e) {
    fail(e, 'Не удалось добавить поле')
  } finally {
    saving.value = false
  }
}

function openEdit(field: FieldDto) {
  editId.value = field.id ?? null
  editNumber.value = field.number ?? ''
  editDialog.value = true
}

async function saveEdit() {
  if (!editId.value) return
  const number = editNumber.value.trim()
  if (!number) return
  try {
    await fieldsApi.update(editId.value, number)
    editDialog.value = false
    toast.add({ severity: 'success', summary: 'Сохранено', life: 2000 })
    await load()
  } catch (e) {
    fail(e, 'Не удалось сохранить')
  }
}

onMounted(load)
</script>

<template>
  <section class="page">
    <h1 class="page__title">Поля</h1>

    <div class="row">
      <PvInputText v-model="newNumber" placeholder="Номер поля" @keyup.enter="add" />
      <PvButton label="Добавить" icon="pi pi-plus" :loading="saving" @click="add" />
    </div>

    <PvDataTable :value="fields" :loading="loading" data-key="id" class="mt">
      <PvColumn field="number" header="Номер" />
      <PvColumn header="" style="width: 6rem">
        <template #body="{ data }">
          <PvButton icon="pi pi-pencil" text rounded @click="openEdit(data)" />
        </template>
      </PvColumn>
    </PvDataTable>

    <PvDialog v-model:visible="editDialog" header="Редактировать поле" modal :style="{ width: '24rem' }">
      <PvInputText v-model="editNumber" class="w-full" @keyup.enter="saveEdit" />
      <template #footer>
        <PvButton label="Отмена" text @click="editDialog = false" />
        <PvButton label="Сохранить" @click="saveEdit" />
      </template>
    </PvDialog>
  </section>
</template>

<style scoped>
.row { display: flex; gap: 0.5rem; align-items: center; }
.mt { margin-top: 1rem; }
.w-full { width: 100%; }
</style>
