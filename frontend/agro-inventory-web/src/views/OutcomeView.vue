<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter, onBeforeRouteLeave } from 'vue-router'
import { useToast } from 'primevue/usetoast'
import { inventoryApi } from '../api/inventory'
import { chemicalsApi } from '../api/chemicals'
import { fieldsApi } from '../api/reference'
import { useReference } from '../composables/useReference'
import { unitLabel } from '../api/types'
import type { OutcomePreviewResponse, OutcomeRequest } from '../api/inventory'
import type { WarehouseStockDto } from '../api/types'
import { nowLocalInput, localToIso } from '../utils/datetime'
import { ApiError } from '../api/http'

const route = useRoute()
const router = useRouter()
const toast = useToast()
const ref_ = useReference()

const chemicalId = ref<string | null>((route.query.chemicalId as string) ?? null)
const warehouseId = ref<string | null>(null)
const cropId = ref<string | null>(null)
const fieldId = ref<string | null>(null)
const newField = ref('')
const quantity = ref<number | null>(null)
const occurredAt = ref(nowLocalInput())
const comment = ref('')

const stock = ref<WarehouseStockDto | null>(null)
const preview = ref<OutcomePreviewResponse | null>(null)
const saving = ref(false)
const done = ref(false)
const doneQty = ref(0)
const dirty = ref(false)

const unit = computed(() => unitLabel(ref_.chemicalUnit(chemicalId.value)))

let previewTimer: ReturnType<typeof setTimeout> | undefined

async function loadStock() {
  stock.value = null
  preview.value = null
  if (!chemicalId.value || !warehouseId.value) return
  try {
    const chem = await chemicalsApi.get(chemicalId.value)
    stock.value = (chem.warehouses ?? []).find((w) => w.warehouseId === warehouseId.value) ?? {
      warehouseId: warehouseId.value, warehouseNumber: '', totalQuantity: 0,
    }
  } catch { /* тихо */ }
}

function baseRequest(): OutcomeRequest {
  return {
    chemicalId: chemicalId.value!,
    warehouseId: warehouseId.value!,
    cropId: cropId.value!,
    fieldId: fieldId.value || null,
    quantity: quantity.value!,
    occurredAt: localToIso(occurredAt.value),
    comment: comment.value.trim() || null,
  } as OutcomeRequest
}

async function runPreview() {
  if (!chemicalId.value || !warehouseId.value || !quantity.value || quantity.value <= 0) {
    preview.value = null
    return
  }
  try {
    preview.value = await inventoryApi.previewOutcome(baseRequest())
  } catch { preview.value = null }
}

watch([quantity], () => {
  dirty.value = true
  clearTimeout(previewTimer)
  previewTimer = setTimeout(runPreview, 300)
})
watch([chemicalId, warehouseId], async () => { dirty.value = true; await loadStock(); await runPreview() })

function fail(e: unknown, fallback: string) {
  toast.add({ severity: 'error', summary: 'Ошибка', detail: e instanceof ApiError ? e.message : fallback, life: 4000 })
}

function valid(): boolean {
  return !!(chemicalId.value && warehouseId.value && cropId.value && quantity.value && quantity.value > 0)
}

async function submit() {
  if (!valid()) {
    toast.add({ severity: 'warn', summary: 'Заполните обязательные поля (в т.ч. культуру)', life: 3000 })
    return
  }
  saving.value = true
  try {
    const res = await inventoryApi.outcome(baseRequest())
    doneQty.value = res.writtenOffQuantity ?? quantity.value!
    dirty.value = false
    done.value = true
    toast.add({ severity: 'success', summary: `Списано ${doneQty.value} ${unit.value}`, life: 2500 })
  } catch (e) {
    fail(e, 'Не удалось списать')
  } finally {
    saving.value = false
  }
}

async function quickAddField() {
  const n = newField.value.trim()
  if (!n) return
  try {
    const f = await fieldsApi.create({ number: n })
    newField.value = ''
    await ref_.reloadFields()
    fieldId.value = f.id ?? null
  } catch (e) { fail(e, 'Не удалось добавить поле') }
}

function addAnother() {
  done.value = false
  warehouseId.value = null
  cropId.value = null
  fieldId.value = null
  quantity.value = null
  comment.value = ''
  occurredAt.value = nowLocalInput()
  stock.value = null
  preview.value = null
  dirty.value = false
}

function close() { router.back() }

onBeforeRouteLeave(() => (dirty.value && !done.value)
  ? window.confirm('У вас есть несохранённые изменения. Выйти без сохранения?') : true)

onMounted(async () => { await ref_.load(); await loadStock() })
</script>

<template>
  <section class="page form">
    <h1 class="page__title">Списание химии</h1>

    <div v-if="done" class="result">
      <PvMessage severity="success" :closable="false">Списано {{ doneQty }} {{ unit }}</PvMessage>
      <div class="row">
        <PvButton label="Закрыть" @click="close" />
        <PvButton label="Добавить ещё" outlined @click="addAnother" />
      </div>
    </div>

    <template v-else>
      <label class="field"><span>Химия *</span>
        <PvSelect v-model="chemicalId" :options="ref_.chemicalOptions.value" option-label="label"
          option-value="value" filter placeholder="Выберите химию" />
      </label>
      <label class="field"><span>Склад *</span>
        <PvSelect v-model="warehouseId" :options="ref_.warehouseOptions.value" option-label="label"
          option-value="value" filter placeholder="Выберите склад" />
      </label>

      <!-- Остаток после выбора склада (ТЗ §11.2) -->
      <div v-if="stock" class="stock">
        <b>Доступно: {{ (stock.totalQuantity ?? 0).toLocaleString('ru-RU') }} {{ unit }}</b>
      </div>

      <label class="field"><span>Культура *</span>
        <PvSelect v-model="cropId" :options="ref_.cropOptions.value" option-label="label"
          option-value="value" filter placeholder="Выберите культуру" />
      </label>

      <label class="field"><span>Номер поля</span>
        <PvSelect v-model="fieldId" :options="ref_.fieldOptions.value" option-label="label"
          option-value="value" filter show-clear placeholder="Выберите поле" />
      </label>
      <div class="quick">
        <PvInputText v-model="newField" placeholder="Быстро добавить поле" @keyup.enter="quickAddField" />
        <PvButton icon="pi pi-plus" text @click="quickAddField" />
      </div>

      <label class="field"><span>Количество, {{ unit }} *</span>
        <PvInputText v-model.number="quantity" type="number" min="0" />
      </label>

      <PvMessage v-if="preview && !preview.sufficient" severity="error" :closable="false">
        Недостаточно остатка: доступно {{ preview.available }} {{ unit }}.
      </PvMessage>

      <label class="field"><span>Дата и время</span>
        <input class="dt" type="datetime-local" v-model="occurredAt" />
      </label>
      <label class="field"><span>Комментарий</span>
        <PvTextarea v-model="comment" rows="2" auto-resize />
      </label>

      <div class="row">
        <PvButton label="Списать" icon="pi pi-check" :loading="saving"
          :disabled="preview !== null && !preview.sufficient" @click="submit" />
        <PvButton label="Отмена" text @click="close" />
      </div>
    </template>
  </section>
</template>

<style scoped>
.form { max-width: 640px; }
.field { display: flex; flex-direction: column; gap: 0.25rem; margin-bottom: 1rem; }
.field > span { font-weight: 600; font-size: 0.9rem; }
.row { display: flex; gap: 0.5rem; align-items: center; }
.quick { display: flex; gap: 0.5rem; align-items: center; margin: -0.5rem 0 1rem; }
.result { display: flex; flex-direction: column; gap: 1rem; }
.stock { background: rgba(0,0,0,0.04); border-radius: 8px; padding: 0.6rem 0.8rem; margin-bottom: 1rem; display: flex; gap: 0.6rem; flex-wrap: wrap; align-items: center; }
.dt { padding: 0.5rem; border: 1px solid #d1d5db; border-radius: 6px; font: inherit; }
/* iOS Safari: нативный datetime-local распирает экран — сдавливаем до контейнера на мобилке */
@media (max-width: 640px) {
  .dt { width: 100%; min-width: 0; max-width: 100%; box-sizing: border-box; -webkit-appearance: none; appearance: none; }
}
</style>
