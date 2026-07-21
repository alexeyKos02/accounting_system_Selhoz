<script setup lang="ts">
import { onMounted, ref, watch, computed } from 'vue'
import { useRoute, useRouter, onBeforeRouteLeave } from 'vue-router'
import { useToast } from 'primevue/usetoast'
import { inventoryApi, CorrectionMode } from '../api/inventory'
import type { CorrectionPreviewResponse, CorrectionRequest } from '../api/inventory'
import { useReference } from '../composables/useReference'
import { unitLabel } from '../api/types'
import { nowLocalInput, localToIso } from '../utils/datetime'
import { ApiError } from '../api/http'

const route = useRoute()
const router = useRouter()
const toast = useToast()
const ref_ = useReference()

const chemicalId = ref<string | null>((route.query.chemicalId as string) ?? null)
const warehouseId = ref<string | null>(null)
const mode = ref<number>(CorrectionMode.SetActual)
const actualTotal = ref<number | null>(null)
const delta = ref<number | null>(null)
const comment = ref('')
const occurredAt = ref(nowLocalInput())
const confirmed = ref(false)

const preview = ref<CorrectionPreviewResponse | null>(null)
const saving = ref(false)
const done = ref(false)
const dirty = ref(false)
let timer: ReturnType<typeof setTimeout> | undefined

const unit = computed(() => unitLabel(ref_.chemicalUnit(chemicalId.value)))

const modeOptions = [
  { label: 'Установить фактический остаток', value: CorrectionMode.SetActual },
  { label: 'Корректировка разницей', value: CorrectionMode.AdjustByDelta },
]

function buildRequest(withConfirm: boolean): CorrectionRequest {
  return {
    chemicalId: chemicalId.value!,
    warehouseId: warehouseId.value!,
    mode: mode.value,
    actualTotalQuantity: mode.value === CorrectionMode.SetActual ? actualTotal.value : null,
    deltaQuantity: mode.value === CorrectionMode.AdjustByDelta ? delta.value : null,
    confirmed: withConfirm,
    occurredAt: localToIso(occurredAt.value),
    comment: comment.value.trim() || null,
  } as CorrectionRequest
}

function ready(): boolean {
  if (!chemicalId.value || !warehouseId.value) return false
  if (mode.value === CorrectionMode.SetActual) return actualTotal.value != null
  return !!delta.value
}

async function runPreview() {
  if (!ready()) { preview.value = null; return }
  try {
    preview.value = await inventoryApi.previewCorrection(buildRequest(false))
  } catch { preview.value = null }
}

watch([chemicalId, warehouseId, mode, actualTotal, delta], () => {
  dirty.value = true
  clearTimeout(timer)
  timer = setTimeout(runPreview, 300)
})

function fail(e: unknown, fallback: string) {
  toast.add({ severity: 'error', summary: 'Ошибка', detail: e instanceof ApiError ? e.message : fallback, life: 4000 })
}

const needsConfirm = computed(() => preview.value?.isBigChange && !confirmed.value)

async function submit() {
  if (!ready()) { toast.add({ severity: 'warn', summary: 'Заполните поля', life: 3000 }); return }
  saving.value = true
  try {
    await inventoryApi.correction(buildRequest(confirmed.value))
    dirty.value = false
    done.value = true
    toast.add({ severity: 'success', summary: 'Корректировка сохранена', life: 2500 })
  } catch (e) {
    fail(e, 'Не удалось сохранить корректировку')
  } finally {
    saving.value = false
  }
}

function close() { router.back() }
onBeforeRouteLeave(() => (dirty.value && !done.value)
  ? window.confirm('У вас есть несохранённые изменения. Выйти без сохранения?') : true)
onMounted(ref_.load)
</script>

<template>
  <section class="page form">
    <h1 class="page__title">Корректировка остатков</h1>

    <div v-if="done" class="result">
      <PvMessage severity="success" :closable="false">Корректировка сохранена</PvMessage>
      <div class="row">
        <PvButton label="Закрыть" @click="close" />
        <PvButton label="Ещё корректировка" outlined @click="done = false" />
      </div>
    </div>

    <template v-else>
      <label class="field"><span>Химия *</span>
        <PvSelect v-model="chemicalId" :options="ref_.chemicalOptions.value" option-label="label" option-value="value" filter placeholder="Химия" />
      </label>
      <label class="field"><span>Склад *</span>
        <PvSelect v-model="warehouseId" :options="ref_.warehouseOptions.value" option-label="label" option-value="value" filter placeholder="Склад" />
      </label>
      <label class="field"><span>Режим</span>
        <PvSelect v-model="mode" :options="modeOptions" option-label="label" option-value="value" />
      </label>

      <label v-if="mode === CorrectionMode.SetActual" class="field"><span>Фактический остаток, {{ unit }} *</span>
        <PvInputText v-model.number="actualTotal" type="number" min="0" />
      </label>

      <label v-else class="field"><span>Разница, {{ unit }} (например -3 или +5) *</span>
        <PvInputText v-model.number="delta" type="number" />
      </label>

      <!-- Было / станет / разница (ТЗ §13.6) -->
      <div v-if="preview" class="preview">
        <div>Было: <b>{{ preview.currentTotal }} {{ unit }}</b></div>
        <div>Станет: <b>{{ preview.newTotal }} {{ unit }}</b></div>
        <div>Разница: <b>{{ (preview.delta ?? 0) > 0 ? '+' : '' }}{{ preview.delta }} {{ unit }}</b></div>
      </div>
      <PvMessage v-if="preview?.isBigChange" severity="warn" :closable="false">
        Большое изменение (более 20%). Подтвердите:
        <label class="confirm"><input type="checkbox" v-model="confirmed" /> подтверждаю</label>
      </PvMessage>

      <label class="field"><span>Комментарий</span><PvTextarea v-model="comment" rows="2" auto-resize /></label>

      <div class="row">
        <PvButton label="Сохранить" icon="pi pi-check" :loading="saving" :disabled="needsConfirm" @click="submit" />
        <PvButton label="Отмена" text @click="close" />
      </div>
    </template>
  </section>
</template>

<style scoped>
.form { max-width: 660px; }
.field { display: flex; flex-direction: column; gap: 0.25rem; margin-bottom: 1rem; }
.field > span { font-weight: 600; font-size: 0.9rem; }
.row { display: flex; gap: 0.5rem; align-items: center; }
.result { display: flex; flex-direction: column; gap: 1rem; }
.preview { background: rgba(0,0,0,0.04); border-radius: 8px; padding: 0.6rem 0.8rem; margin-bottom: 0.75rem; display: flex; gap: 1.5rem; flex-wrap: wrap; }
.confirm { display: inline-flex; gap: 0.35rem; align-items: center; margin-left: 0.5rem; }
</style>
