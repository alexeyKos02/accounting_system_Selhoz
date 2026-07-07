<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useToast } from 'primevue/usetoast'
import { settingsApi } from '../api/settings'
import { ApiError } from '../api/http'

const toast = useToast()
const threshold = ref<number>(10)
const autoOpen = ref<boolean>(false)
const updatedAt = ref<string | null>(null)
const loading = ref(false)
const saving = ref(false)

async function load() {
  loading.value = true
  try {
    const s = await settingsApi.get()
    threshold.value = s.lowStockThresholdLiters ?? 10
    autoOpen.value = s.autoOpenPackages ?? false
    updatedAt.value = s.updatedAt ?? null
  } finally {
    loading.value = false
  }
}

async function save() {
  if (threshold.value == null || threshold.value < 0) {
    toast.add({ severity: 'warn', summary: 'Порог не может быть отрицательным', life: 3000 })
    return
  }
  saving.value = true
  try {
    const s = await settingsApi.update({
      lowStockThresholdLiters: threshold.value,
      autoOpenPackages: autoOpen.value,
    })
    updatedAt.value = s.updatedAt ?? null
    toast.add({ severity: 'success', summary: 'Настройки сохранены', life: 2500 })
  } catch (e) {
    toast.add({
      severity: 'error', summary: 'Ошибка',
      detail: e instanceof ApiError ? e.message : 'Не удалось сохранить настройки', life: 4000,
    })
  } finally {
    saving.value = false
  }
}

function fmtDate(iso: string) {
  return new Date(iso).toLocaleString('ru-RU')
}

onMounted(load)
</script>

<template>
  <section class="page form">
    <h1 class="page__title">Настройки</h1>

    <label class="field">
      <span>Порог малого остатка, л</span>
      <PvInputText v-model.number="threshold" type="number" min="0" :disabled="loading" />
      <small>Химия с остатком ниже этого значения попадает в блок «Малый остаток» на дашборде.</small>
    </label>

    <label class="field field--row">
      <PvToggleSwitch v-model="autoOpen" :disabled="loading" />
      <span class="field__inline">
        <b>Автоматически вскрывать упаковки при списании</b>
        <small>Если не хватает наливного остатка — вскрывать новую упаковку без подтверждения.</small>
      </span>
    </label>

    <div class="row">
      <PvButton label="Сохранить" icon="pi pi-check" :loading="saving" @click="save" />
      <span v-if="updatedAt" class="upd">Обновлено: {{ fmtDate(updatedAt) }}</span>
    </div>
  </section>
</template>

<style scoped>
.form { max-width: 560px; }
.field { display: flex; flex-direction: column; gap: 0.3rem; margin-bottom: 1.5rem; }
.field > span { font-weight: 600; font-size: 0.9rem; }
.field small { color: #6b7280; }
.field--row { flex-direction: row; align-items: flex-start; gap: 0.75rem; }
.field__inline { display: flex; flex-direction: column; gap: 0.2rem; }
.row { display: flex; align-items: center; gap: 1rem; }
.upd { color: #6b7280; font-size: 0.85rem; }
</style>
