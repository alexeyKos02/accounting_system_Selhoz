<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useToast } from 'primevue/usetoast'
import { cropsApi } from '../api/reference'
import type { CropDto } from '../api/types'
import { ApiError } from '../api/http'

const toast = useToast()
const crops = ref<CropDto[]>([])
const loading = ref(false)
const newName = ref('')
const saving = ref(false)

const editDialog = ref(false)
const editId = ref<string | null>(null)
const editName = ref('')

async function load() {
  loading.value = true
  try {
    crops.value = await cropsApi.list()
  } finally {
    loading.value = false
  }
}

function fail(e: unknown, fallback: string) {
  const msg = e instanceof ApiError ? e.message : fallback
  toast.add({ severity: 'error', summary: 'Ошибка', detail: msg, life: 4000 })
}

async function add() {
  const name = newName.value.trim()
  if (!name) return
  saving.value = true
  try {
    await cropsApi.create(name)
    newName.value = ''
    toast.add({ severity: 'success', summary: 'Культура добавлена', life: 2000 })
    await load()
  } catch (e) {
    fail(e, 'Не удалось добавить культуру')
  } finally {
    saving.value = false
  }
}

function openEdit(crop: CropDto) {
  editId.value = crop.id ?? null
  editName.value = crop.name ?? ''
  editDialog.value = true
}

async function saveEdit() {
  if (!editId.value) return
  const name = editName.value.trim()
  if (!name) return
  try {
    await cropsApi.update(editId.value, name)
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
    <h1 class="page__title">Культуры</h1>

    <div class="row">
      <PvInputText v-model="newName" placeholder="Название культуры" @keyup.enter="add" />
      <PvButton label="Добавить" icon="pi pi-plus" :loading="saving" @click="add" />
    </div>

    <PvDataTable :value="crops" :loading="loading" data-key="id" class="mt">
      <PvColumn field="name" header="Название" />
      <PvColumn header="" style="width: 6rem">
        <template #body="{ data }">
          <PvButton icon="pi pi-pencil" text rounded @click="openEdit(data)" />
        </template>
      </PvColumn>
    </PvDataTable>

    <PvDialog v-model:visible="editDialog" header="Редактировать культуру" modal :style="{ width: '24rem' }">
      <PvInputText v-model="editName" class="w-full" @keyup.enter="saveEdit" />
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
