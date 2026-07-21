<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useToast } from 'primevue/usetoast'
import { canonicalApi } from '../api/catalog'
import { cropsApi } from '../api/reference'
import type { CanonicalChemicalDto, ChemicalTypeValue, CropDto } from '../api/types'
import { chemicalTypeLabels, chemicalTypeOptions, MeasureUnit, measureUnitOptions, unitLabel } from '../api/types'
import { ApiError } from '../api/http'

// Общий канонический справочник препаратов (ТЗ §12). Ведёт только SystemAdmin.
// Набор полей повторяет карточку химии: тип, единица измерения, производитель, культуры, комментарий.
const toast = useToast()
const items = ref<CanonicalChemicalDto[]>([])
const loading = ref(false)
const search = ref('')

const crops = ref<CropDto[]>([])
const cropOptions = computed(() => crops.value.map((c) => ({ label: c.name, value: c.id })))
const newCropName = ref('')

const dialog = ref(false)
const editId = ref<string | null>(null)
const form = ref({
  canonicalName: '',
  type: null as ChemicalTypeValue | null,
  measureUnit: MeasureUnit.Liter as 1 | 2,
  manufacturer: '',
  comment: '',
  cropIds: [] as string[],
})
const saving = ref(false)

function fail(e: unknown, fallback: string) {
  toast.add({ severity: 'error', summary: 'Ошибка', detail: e instanceof ApiError ? e.message : fallback, life: 4000 })
}

function cropNames(c: CanonicalChemicalDto): string {
  return (c.crops ?? []).map((x) => x.name).join(', ')
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

function emptyForm() {
  return {
    canonicalName: '', type: null as ChemicalTypeValue | null,
    measureUnit: MeasureUnit.Liter as 1 | 2, manufacturer: '', comment: '', cropIds: [] as string[],
  }
}

function openCreate() {
  editId.value = null
  form.value = emptyForm()
  dialog.value = true
}

function openEdit(c: CanonicalChemicalDto) {
  editId.value = c.id ?? null
  form.value = {
    canonicalName: c.canonicalName ?? '',
    type: c.type ?? null,
    measureUnit: (c.measureUnit ?? MeasureUnit.Liter) as 1 | 2,
    manufacturer: c.manufacturer ?? '',
    comment: c.comment ?? '',
    cropIds: (c.crops ?? []).map((x) => x.id!),
  }
  dialog.value = true
}

async function quickAddCrop() {
  const n = newCropName.value.trim()
  if (!n) return
  try {
    const crop = await cropsApi.create(n)
    newCropName.value = ''
    crops.value = await cropsApi.list()
    if (crop.id) form.value.cropIds = [...form.value.cropIds, crop.id]
  } catch (e) {
    fail(e, 'Не удалось добавить культуру')
  }
}

async function save() {
  saving.value = true
  const body = {
    canonicalName: form.value.canonicalName.trim(),
    type: form.value.type ?? undefined,
    measureUnit: form.value.measureUnit,
    manufacturer: form.value.manufacturer.trim() || null,
    comment: form.value.comment.trim() || null,
    cropIds: form.value.cropIds,
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

onMounted(async () => {
  await load()
  try { crops.value = await cropsApi.list() } catch { crops.value = [] }
})
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
      <PvColumn header="Тип средства">
        <template #body="{ data }">{{ data.type != null ? chemicalTypeLabels[data.type] : '—' }}</template>
      </PvColumn>
      <PvColumn header="Ед. изм.">
        <template #body="{ data }">{{ unitLabel(data.measureUnit) }}</template>
      </PvColumn>
      <PvColumn field="manufacturer" header="Производитель" />
      <PvColumn header="Культуры">
        <template #body="{ data }">{{ cropNames(data) || '—' }}</template>
      </PvColumn>
      <PvColumn header="" style="width: 5rem">
        <template #body="{ data }">
          <PvButton icon="pi pi-pencil" text rounded @click="openEdit(data)" />
        </template>
      </PvColumn>
    </PvDataTable>

    <PvDialog v-model:visible="dialog" :header="editId ? 'Изменить препарат' : 'Новый препарат'" modal :style="{ width: '32rem' }">
      <div class="form">
        <label class="form__field"><span>Название *</span><PvInputText v-model="form.canonicalName" placeholder="Например, Раундап" /></label>

        <label class="form__field">
          <span>Тип средства</span>
          <PvSelect v-model="form.type" :options="chemicalTypeOptions" option-label="label"
            option-value="value" placeholder="Не указан" show-clear />
        </label>

        <label class="form__field">
          <span>Единица измерения *</span>
          <PvSelect v-model="form.measureUnit" :options="measureUnitOptions" option-label="label" option-value="value" />
          <small class="hint">Литры — для жидкой химии, килограммы — для сухой.</small>
        </label>

        <label class="form__field"><span>Производитель</span><PvInputText v-model="form.manufacturer" /></label>

        <label class="form__field">
          <span>Культуры</span>
          <PvMultiSelect v-model="form.cropIds" :options="cropOptions" option-label="label"
            option-value="value" placeholder="Выберите культуры" filter />
        </label>

        <div class="quick">
          <PvInputText v-model="newCropName" placeholder="Быстро добавить культуру" @keyup.enter="quickAddCrop" />
          <PvButton icon="pi pi-plus" text @click="quickAddCrop" />
        </div>

        <label class="form__field"><span>Комментарий</span><PvTextarea v-model="form.comment" rows="3" auto-resize /></label>
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
.quick { display: flex; gap: 0.5rem; align-items: center; margin-top: -0.3rem; }
.hint { color: var(--p-text-muted-color, #6b7280); font-weight: 400; font-size: 0.8rem; }
</style>
