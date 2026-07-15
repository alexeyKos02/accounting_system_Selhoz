<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRoute, useRouter, onBeforeRouteLeave } from 'vue-router'
import { useToast } from 'primevue/usetoast'
import { inventoryApi, Unit } from '../api/inventory'
import type { IncomeRequest } from '../api/inventory'
import { warehousesApi } from '../api/reference'
import { gptApi } from '../api/gpt'
import type { OperationSuggestionDto } from '../api/gpt'
import GptParseDialog from '../components/GptParseDialog.vue'
import { useReference } from '../composables/useReference'
import { nowLocalInput, localToIso } from '../utils/datetime'
import { ApiError } from '../api/http'
import { MovementType } from '../api/history'

const route = useRoute()
const router = useRouter()
const toast = useToast()
const ref_ = useReference()

const unitOptions = [
  { label: 'Литры', value: Unit.Liter },
  { label: 'Банки', value: Unit.Can },
  { label: 'Штуки', value: Unit.Piece },
]

const chemicalId = ref<string | null>((route.query.chemicalId as string) ?? null)
const warehouseId = ref<string | null>(null)
const unit = ref<number>(Unit.Liter)
const quantity = ref<number | null>(null)
const packageVolume = ref<number | null>(null)
const occurredAt = ref(nowLocalInput())
const comment = ref('')
const saving = ref(false)
const done = ref(false)
const dirty = ref(false)

const newWarehouse = ref('')

const gptConfigured = ref(false)
const gptDialog = ref(false)

function markDirty() { dirty.value = true }

// Применяем распознанное предложение к форме (ТЗ §26). Пользователь проверяет и сохраняет сам.
function applySuggestion(s: OperationSuggestionDto) {
  const notes: string[] = []

  if (s.operationType === MovementType.Outcome)
    notes.push('Похоже на списание — эта форма для прихода, проверьте тип операции.')

  if (s.chemical?.id) chemicalId.value = s.chemical.id
  else if (s.chemical?.name) notes.push(`Химия «${s.chemical.name}» не найдена — выберите вручную.`)

  if (s.warehouse?.id) warehouseId.value = s.warehouse.id
  else if (s.warehouse?.name) notes.push(`Склад «${s.warehouse.name}» не найден — выберите вручную.`)

  if (s.unit) unit.value = s.unit
  if (s.quantity != null) quantity.value = s.quantity
  if (s.packageVolumeLiters != null) packageVolume.value = s.packageVolumeLiters
  if (s.comment) comment.value = s.comment
  if (s.notes) notes.push(s.notes)

  dirty.value = true
  toast.add({
    severity: 'success', summary: 'Данные распознаны',
    detail: notes.length ? notes.join(' ') : 'Проверьте поля и сохраните.', life: 5000,
  })
}

function fail(e: unknown, fallback: string) {
  toast.add({ severity: 'error', summary: 'Ошибка', detail: e instanceof ApiError ? e.message : fallback, life: 4000 })
}

async function quickAddWarehouse() {
  const n = newWarehouse.value.trim()
  if (!n) return
  try {
    const w = await warehousesApi.create(n)
    newWarehouse.value = ''
    await ref_.reloadWarehouses()
    warehouseId.value = w.id ?? null
  } catch (e) { fail(e, 'Не удалось добавить склад') }
}

function valid(): boolean {
  if (!chemicalId.value || !warehouseId.value || !quantity.value || quantity.value <= 0) return false
  if (unit.value !== Unit.Liter && (!packageVolume.value || packageVolume.value <= 0)) return false
  return true
}

async function submit() {
  if (!valid()) {
    toast.add({ severity: 'warn', summary: 'Заполните обязательные поля', life: 3000 })
    return
  }
  saving.value = true
  try {
    await inventoryApi.income({
      chemicalId: chemicalId.value!,
      warehouseId: warehouseId.value!,
      unit: unit.value,
      quantity: quantity.value!,
      packageVolumeLiters: unit.value === Unit.Liter ? null : packageVolume.value!,
      occurredAt: localToIso(occurredAt.value),
      comment: comment.value.trim() || null,
    } as unknown as IncomeRequest)
    dirty.value = false
    done.value = true
    toast.add({ severity: 'success', summary: 'Приход добавлен', life: 2500 })
  } catch (e) {
    fail(e, 'Не удалось добавить приход')
  } finally {
    saving.value = false
  }
}

// «Добавить ещё» — оставляем только химию (ТЗ §12)
function addAnother() {
  done.value = false
  warehouseId.value = null
  quantity.value = null
  packageVolume.value = null
  unit.value = Unit.Liter
  comment.value = ''
  occurredAt.value = nowLocalInput()
  dirty.value = false
}

function close() { router.back() }

onBeforeRouteLeave(() => (dirty.value && !done.value)
  ? window.confirm('У вас есть несохранённые изменения. Выйти без сохранения?') : true)

onMounted(async () => {
  await ref_.load()
  try { gptConfigured.value = (await gptApi.status()).configured } catch { gptConfigured.value = false }
})
</script>

<template>
  <section class="page form">
    <div class="head">
      <h1 class="page__title">Приход химии</h1>
      <div class="head__actions">
        <PvButton label="В несколько хозяйств" icon="pi pi-building" outlined
          @click="router.push({ name: 'income-bulk' })" />
        <PvButton v-if="gptConfigured && !done" label="Распознать (ИИ)" icon="pi pi-sparkles"
          outlined @click="gptDialog = true" />
      </div>
    </div>

    <GptParseDialog v-model:visible="gptDialog" @apply="applySuggestion" />

    <div v-if="done" class="result">
      <PvMessage severity="success" :closable="false">Приход добавлен</PvMessage>
      <div class="row">
        <PvButton label="Закрыть" @click="close" />
        <PvButton label="Добавить ещё" outlined @click="addAnother" />
      </div>
    </div>

    <template v-else>
      <label class="field"><span>Химия *</span>
        <PvSelect v-model="chemicalId" :options="ref_.chemicalOptions.value" option-label="label"
          option-value="value" filter placeholder="Выберите химию" @change="markDirty" />
      </label>

      <label class="field"><span>Склад *</span>
        <PvSelect v-model="warehouseId" :options="ref_.warehouseOptions.value" option-label="label"
          option-value="value" filter placeholder="Выберите склад" @change="markDirty" />
      </label>
      <div class="quick">
        <PvInputText v-model="newWarehouse" placeholder="Быстро добавить склад" @keyup.enter="quickAddWarehouse" />
        <PvButton icon="pi pi-plus" text @click="quickAddWarehouse" />
      </div>

      <label class="field"><span>Единица *</span>
        <PvSelect v-model="unit" :options="unitOptions" option-label="label" option-value="value" @change="markDirty" />
      </label>

      <div class="grid2">
        <label class="field"><span>{{ unit === Unit.Liter ? 'Количество, л *' : 'Количество упаковок *' }}</span>
          <PvInputText v-model.number="quantity" type="number" min="0" @input="markDirty" />
        </label>
        <label v-if="unit !== Unit.Liter" class="field"><span>Литраж упаковки, л *</span>
          <PvInputText v-model.number="packageVolume" type="number" min="0" @input="markDirty" />
        </label>
      </div>

      <label class="field"><span>Дата и время</span>
        <input class="dt" type="datetime-local" v-model="occurredAt" @change="markDirty" />
      </label>

      <label class="field"><span>Комментарий</span>
        <PvTextarea v-model="comment" rows="2" auto-resize @input="markDirty" />
      </label>

      <div class="row">
        <PvButton label="Сохранить" icon="pi pi-check" :loading="saving" @click="submit" />
        <PvButton label="Отмена" text @click="close" />
      </div>
    </template>
  </section>
</template>

<style scoped>
.form { max-width: 620px; }
.head { display: flex; justify-content: space-between; align-items: center; gap: 1rem; }
.head__actions { display: flex; gap: 0.5rem; align-items: center; flex-wrap: wrap; justify-content: flex-end; }
.field { display: flex; flex-direction: column; gap: 0.25rem; margin-bottom: 1rem; }
.field > span { font-weight: 600; font-size: 0.9rem; }
.grid2 { display: grid; grid-template-columns: 1fr 1fr; gap: 0.75rem; }
.quick { display: flex; gap: 0.5rem; align-items: center; margin: -0.5rem 0 1rem; }
.row { display: flex; gap: 0.5rem; align-items: center; }
.result { display: flex; flex-direction: column; gap: 1rem; }
.dt { padding: 0.5rem; border: 1px solid var(--p-inputtext-border-color, #d1d5db); border-radius: 6px; font: inherit; }

/* Мобилка: числовые поля не должны распирать экран (min-width инпутов) */
@media (max-width: 640px) {
  .head { align-items: flex-start; flex-direction: column; }
  .head__actions { justify-content: flex-start; }
  .grid2 { grid-template-columns: 1fr; }
  .grid2 :deep(.p-inputtext) { min-width: 0; width: 100%; }
  /* iOS Safari: нативный datetime-local имеет большую min-width — сдавливаем до контейнера */
  .dt { width: 100%; min-width: 0; max-width: 100%; box-sizing: border-box; -webkit-appearance: none; appearance: none; }
}
</style>
