<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useToast } from 'primevue/usetoast'
import { canonicalApi } from '../api/catalog'
import type { CanonicalChemicalDto } from '../api/types'
import { ApiError } from '../api/http'

// Общий канонический справочник препаратов (ТЗ §12). Ведёт только SystemAdmin.
const toast = useToast()
const items = ref<CanonicalChemicalDto[]>([])
const loading = ref(false)
const search = ref('')

const dialog = ref(false)
const editId = ref<string | null>(null)
const form = ref({
  canonicalName: '', manufacturer: '', activeIngredient: '', concentration: '', formulation: '', registrationNumber: '',
})
const saving = ref(false)

function fail(e: unknown, fallback: string) {
  toast.add({ severity: 'error', summary: 'Ошибка', detail: e instanceof ApiError ? e.message : fallback, life: 4000 })
}

async function load() {
  loading.value = true
  try {
    items.value = await canonicalApi.list(search.value.trim() || undefined)
  } catch (e) {
    fail(e, 'Не удалось загрузить каталог')
  } finally {
    loading.value = false
  }
}

function openCreate() {
  editId.value = null
  form.value = { canonicalName: '', manufacturer: '', activeIngredient: '', concentration: '', formulation: '', registrationNumber: '' }
  dialog.value = true
}

function openEdit(c: CanonicalChemicalDto) {
  editId.value = c.id ?? null
  form.value = {
    canonicalName: c.canonicalName ?? '',
    manufacturer: c.manufacturer ?? '',
    activeIngredient: c.activeIngredient ?? '',
    concentration: c.concentration ?? '',
    formulation: c.formulation ?? '',
    registrationNumber: c.registrationNumber ?? '',
  }
  dialog.value = true
}

async function save() {
  saving.value = true
  const body = {
    canonicalName: form.value.canonicalName.trim(),
    manufacturer: form.value.manufacturer.trim() || null,
    activeIngredient: form.value.activeIngredient.trim() || null,
    concentration: form.value.concentration.trim() || null,
    formulation: form.value.formulation.trim() || null,
    registrationNumber: form.value.registrationNumber.trim() || null,
  }
  try {
    if (editId.value) await canonicalApi.update(editId.value, body)
    else await canonicalApi.create(body)
    dialog.value = false
    toast.add({ severity: 'success', summary: 'Сохранено', life: 2000 })
    await load()
  } catch (e) {
    fail(e, 'Не удалось сохранить')
  } finally {
    saving.value = false
  }
}

onMounted(load)
</script>

<template>
  <section class="page">
    <div class="head">
      <h1 class="page__title">Общий каталог препаратов</h1>
      <PvButton label="Добавить" icon="pi pi-plus" @click="openCreate" />
    </div>
    <p class="muted">Эталонный справочник, общий для всех хозяйств. По нему объединяется одинаковая химия в режиме «Все хозяйства» (§17).</p>

    <div class="row mt">
      <PvInputText v-model="search" placeholder="Поиск по названию" @keyup.enter="load" />
      <PvButton label="Найти" icon="pi pi-search" outlined @click="load" />
    </div>

    <PvDataTable :value="items" :loading="loading" data-key="id" class="mt">
      <PvColumn field="canonicalName" header="Название" />
      <PvColumn field="manufacturer" header="Производитель" />
      <PvColumn field="activeIngredient" header="Действующее вещество" />
      <PvColumn field="concentration" header="Концентрация" />
      <PvColumn header="" style="width: 5rem">
        <template #body="{ data }">
          <PvButton icon="pi pi-pencil" text rounded @click="openEdit(data)" />
        </template>
      </PvColumn>
    </PvDataTable>

    <PvDialog v-model:visible="dialog" :header="editId ? 'Изменить препарат' : 'Новый препарат'" modal :style="{ width: '30rem' }">
      <div class="form">
        <label class="form__field"><span>Название *</span><PvInputText v-model="form.canonicalName" /></label>
        <label class="form__field"><span>Производитель</span><PvInputText v-model="form.manufacturer" /></label>
        <label class="form__field"><span>Действующее вещество</span><PvInputText v-model="form.activeIngredient" /></label>
        <label class="form__field"><span>Концентрация</span><PvInputText v-model="form.concentration" /></label>
        <label class="form__field"><span>Формуляция</span><PvInputText v-model="form.formulation" /></label>
        <label class="form__field"><span>Рег. номер</span><PvInputText v-model="form.registrationNumber" /></label>
      </div>
      <template #footer>
        <PvButton label="Отмена" text @click="dialog = false" />
        <PvButton label="Сохранить" :loading="saving" :disabled="!form.canonicalName.trim()" @click="save" />
      </template>
    </PvDialog>
  </section>
</template>

<style scoped>
.head { display: flex; justify-content: space-between; align-items: center; }
.muted { color: var(--p-text-muted-color, #6b7280); font-size: 0.9rem; margin: 0.5rem 0 0; }
.row { display: flex; gap: 0.5rem; align-items: center; }
.mt { margin-top: 1rem; }
.form { display: flex; flex-direction: column; gap: 0.7rem; }
.form__field { display: flex; flex-direction: column; gap: 0.3rem; font-size: 0.9rem; }
.form__field :deep(input) { width: 100%; }
</style>
