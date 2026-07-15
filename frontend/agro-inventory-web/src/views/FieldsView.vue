<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useToast } from 'primevue/usetoast'
import { cropsApi, fieldsApi } from '../api/reference'
import type { CropDto, FieldDto } from '../api/types'
import { ApiError } from '../api/http'

const toast = useToast()
const fields = ref<FieldDto[]>([])
const crops = ref<CropDto[]>([])
const loading = ref(false)
const newNumber = ref('')
const newArea = ref<number | null>(null)
const newCropId = ref<string | null>(null)
const saving = ref(false)

const editDialog = ref(false)
const editId = ref<string | null>(null)
const editNumber = ref('')
const editArea = ref<number | null>(null)
const editCropId = ref<string | null>(null)

const cropOptions = () => crops.value.map((c) => ({ label: c.name, value: c.id }))

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
    await fieldsApi.create({ number, areaHectares: newArea.value, currentCropId: newCropId.value })
    newNumber.value = ''
    newArea.value = null
    newCropId.value = null
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
  editArea.value = field.areaHectares ?? null
  editCropId.value = field.currentCropId ?? null
  editDialog.value = true
}

async function saveEdit() {
  if (!editId.value) return
  const number = editNumber.value.trim()
  if (!number) return
  try {
    await fieldsApi.update(editId.value, { number, areaHectares: editArea.value, currentCropId: editCropId.value })
    editDialog.value = false
    toast.add({ severity: 'success', summary: 'Сохранено', life: 2000 })
    await load()
  } catch (e) {
    fail(e, 'Не удалось сохранить')
  }
}

onMounted(async () => {
  ;[crops.value] = await Promise.all([cropsApi.list()])
  await load()
})
</script>

<template>
  <section class="page">
    <h1 class="page__title">Поля</h1>

    <div class="field-form">
      <PvInputText v-model="newNumber" placeholder="Номер поля" @keyup.enter="add" />
      <PvInputText v-model.number="newArea" type="number" min="0" placeholder="Площадь, га" @keyup.enter="add" />
      <PvSelect
        v-model="newCropId"
        :options="cropOptions()"
        option-label="label"
        option-value="value"
        filter
        show-clear
        placeholder="Текущая культура"
      />
      <PvButton label="Добавить" icon="pi pi-plus" :loading="saving" @click="add" />
    </div>

    <PvDataTable :value="fields" :loading="loading" data-key="id" class="mt">
      <PvColumn field="number" header="Номер" />
      <PvColumn header="Площадь">
        <template #body="{ data }">
          <span v-if="data.areaHectares != null">{{ data.areaHectares }} га</span>
          <span v-else class="muted">—</span>
        </template>
      </PvColumn>
      <PvColumn header="Текущая культура">
        <template #body="{ data }">
          <span v-if="data.currentCropName">{{ data.currentCropName }}</span>
          <span v-else class="muted">—</span>
        </template>
      </PvColumn>
      <PvColumn header="" style="width: 6rem">
        <template #body="{ data }">
          <PvButton icon="pi pi-pencil" text rounded @click="openEdit(data)" />
        </template>
      </PvColumn>
    </PvDataTable>

    <PvDialog v-model:visible="editDialog" header="Редактировать поле" modal :style="{ width: '24rem' }">
      <div class="dialog-fields">
        <label class="field"><span>Номер поля</span><PvInputText v-model="editNumber" class="w-full" @keyup.enter="saveEdit" /></label>
        <label class="field"><span>Площадь, га</span><PvInputText v-model.number="editArea" type="number" min="0" class="w-full" /></label>
        <label class="field"><span>Текущая культура</span>
          <PvSelect
            v-model="editCropId"
            :options="cropOptions()"
            option-label="label"
            option-value="value"
            filter
            show-clear
            class="w-full"
            placeholder="Не указано"
          />
        </label>
      </div>
      <template #footer>
        <PvButton label="Отмена" text @click="editDialog = false" />
        <PvButton label="Сохранить" @click="saveEdit" />
      </template>
    </PvDialog>
  </section>
</template>

<style scoped>
.field-form { display: grid; grid-template-columns: minmax(10rem, 1fr) minmax(8rem, 12rem) minmax(12rem, 1fr) auto; gap: 0.5rem; align-items: center; }
.mt { margin-top: 1rem; }
.w-full { width: 100%; }
.muted { color: #6b7280; }
.dialog-fields { display: flex; flex-direction: column; gap: 0.75rem; }
.field { display: flex; flex-direction: column; gap: 0.25rem; }
.field > span { font-weight: 600; font-size: 0.9rem; }
@media (max-width: 640px) {
  .field-form { grid-template-columns: 1fr; }
}
</style>
