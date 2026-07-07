<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter, onBeforeRouteLeave } from 'vue-router'
import { useToast } from 'primevue/usetoast'
import { inventoryApi } from '../api/inventory'
import { chemicalsApi } from '../api/chemicals'
import { useReference } from '../composables/useReference'
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
const quantity = ref<number | null>(null)
const mode = ref<'auto' | 'manual'>('auto')
const sourceKey = ref<string | null>(null) // "type:id"
const occurredAt = ref(nowLocalInput())
const comment = ref('')

const stock = ref<WarehouseStockDto | null>(null)
const preview = ref<OutcomePreviewResponse | null>(null)
const showPlan = ref(false)
const saving = ref(false)
const done = ref(false)
const doneLiters = ref(0)
const dirty = ref(false)

const confirmOpenDialog = ref(false)

let previewTimer: ReturnType<typeof setTimeout> | undefined

// Источники для ручного выбора (ТЗ §11.3)
const sourceOptions = computed(() => {
  const s = stock.value
  if (!s) return []
  const opts: { label: string; value: string }[] = []
  if ((s.looseLiters ?? 0) > 0) opts.push({ label: `Наливом (${s.looseLiters} л)`, value: '1:' })
  for (const o of s.openedPackages ?? [])
    opts.push({ label: `Вскрытая ${o.initialLiters} л — осталось ${o.remainingLiters} л`, value: `3:${o.id}` })
  for (const g of s.packageGroups ?? [])
    opts.push({ label: `Полные ${g.quantity} × ${g.packageVolumeLiters} л`, value: `2:${g.id}` })
  return opts
})

function buildSource(): OutcomeRequest['source'] | undefined {
  if (mode.value !== 'manual' || !sourceKey.value) return undefined
  const [type, id] = sourceKey.value.split(':')
  return { type: Number(type), id: id || null } as unknown as OutcomeRequest['source']
}

async function loadStock() {
  stock.value = null
  preview.value = null
  if (!chemicalId.value || !warehouseId.value) return
  try {
    const chem = await chemicalsApi.get(chemicalId.value)
    stock.value = (chem.warehouses ?? []).find((w) => w.warehouseId === warehouseId.value) ?? {
      warehouseId: warehouseId.value, warehouseNumber: '', totalLiters: 0, looseLiters: 0,
      packageGroups: [], openedPackages: [],
    }
  } catch { /* тихо */ }
}

function baseRequest(allowOpen: boolean): OutcomeRequest {
  return {
    chemicalId: chemicalId.value!,
    warehouseId: warehouseId.value!,
    cropId: cropId.value!,
    quantityLiters: quantity.value!,
    source: buildSource(),
    allowOpenNewPackage: allowOpen,
    occurredAt: localToIso(occurredAt.value),
    comment: comment.value.trim() || null,
  } as unknown as OutcomeRequest
}

async function runPreview() {
  if (!chemicalId.value || !warehouseId.value || !quantity.value || quantity.value <= 0) {
    preview.value = null
    return
  }
  try {
    preview.value = await inventoryApi.previewOutcome(baseRequest(false))
  } catch { preview.value = null }
}

watch([quantity, sourceKey, mode], () => {
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
  // Подтверждение вскрытия новой упаковки (ТЗ §11.9)
  if (preview.value?.requiresOpenConfirmation) {
    confirmOpenDialog.value = true
    return
  }
  await commit(false)
}

async function commit(allowOpen: boolean) {
  confirmOpenDialog.value = false
  saving.value = true
  try {
    const res = await inventoryApi.outcome(baseRequest(allowOpen))
    doneLiters.value = res.writtenOffLiters ?? quantity.value!
    dirty.value = false
    done.value = true
    toast.add({ severity: 'success', summary: `Списано ${doneLiters.value} л`, life: 2500 })
  } catch (e) {
    fail(e, 'Не удалось списать')
  } finally {
    saving.value = false
  }
}

function addAnother() {
  done.value = false
  warehouseId.value = null
  cropId.value = null
  quantity.value = null
  mode.value = 'auto'
  sourceKey.value = null
  comment.value = ''
  occurredAt.value = nowLocalInput()
  stock.value = null
  preview.value = null
  dirty.value = false
}

function close() { router.back() }

const sourceTypeLabel = (t: number) => (t === 1 ? 'Наливом' : t === 2 ? 'Полная упаковка' : 'Вскрытая упаковка')

onBeforeRouteLeave(() => (dirty.value && !done.value)
  ? window.confirm('У вас есть несохранённые изменения. Выйти без сохранения?') : true)

onMounted(async () => { await ref_.load(); await loadStock() })
</script>

<template>
  <section class="page form">
    <h1 class="page__title">Списание химии</h1>

    <div v-if="done" class="result">
      <PvMessage severity="success" :closable="false">Списано {{ doneLiters }} л</PvMessage>
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
        <b>Доступно: {{ (stock.totalLiters ?? 0).toLocaleString('ru-RU') }} л</b>
        <span>наливом {{ stock.looseLiters ?? 0 }} л</span>
        <span v-if="stock.packageGroups?.length">· полные: {{stock.packageGroups.map(g => `${g.quantity}×${g.packageVolumeLiters}`).join(', ')}}</span>
        <span v-if="stock.openedPackages?.length">· вскрытые: {{stock.openedPackages.map(o => `${o.remainingLiters}`).join(', ')}} л</span>
      </div>

      <label class="field"><span>Культура *</span>
        <PvSelect v-model="cropId" :options="ref_.cropOptions.value" option-label="label"
          option-value="value" filter placeholder="Выберите культуру" />
      </label>

      <label class="field"><span>Количество, л *</span>
        <PvInputText v-model.number="quantity" type="number" min="0" />
      </label>

      <label class="field"><span>Источник</span>
        <PvSelect v-model="mode" :options="[{label:'Автоматический добор',value:'auto'},{label:'Выбрать вручную',value:'manual'}]"
          option-label="label" option-value="value" />
      </label>
      <label v-if="mode === 'manual'" class="field"><span>Из чего списать</span>
        <PvSelect v-model="sourceKey" :options="sourceOptions" option-label="label" option-value="value" placeholder="Источник" />
      </label>

      <!-- Предупреждения (ТЗ §11.8, §11.9) -->
      <PvMessage v-if="preview && !preview.sufficient" severity="error" :closable="false">
        Недостаточно остатка: доступно {{ preview.available }} л.
      </PvMessage>
      <PvMessage v-else-if="preview?.topUpUsed" severity="warn" :closable="false">
        Выбранного источника не хватает — остаток будет добран автоматически.
      </PvMessage>
      <PvMessage v-else-if="preview?.willOpenNewPackage" severity="warn" :closable="false">
        Для списания будет вскрыта новая упаковка.
      </PvMessage>

      <div v-if="preview?.steps?.length" class="plan">
        <a href="#" @click.prevent="showPlan = !showPlan">{{ showPlan ? 'Скрыть план' : 'Показать план списания' }}</a>
        <ul v-if="showPlan">
          <li v-for="(s, i) in preview.steps" :key="i">
            {{ sourceTypeLabel(s.sourceType!) }}: {{ s.liters }} л
            <span v-if="s.opensNewPackage">(вскрытие, остаток {{ s.openedRemainder }} л)</span>
          </li>
        </ul>
      </div>

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

    <PvDialog v-model:visible="confirmOpenDialog" header="Вскрыть новую упаковку?" modal :style="{ width: '26rem' }">
      <p>Для списания {{ quantity }} л нужно вскрыть новую полную упаковку. Продолжить?</p>
      <template #footer>
        <PvButton label="Отмена" text @click="confirmOpenDialog = false" />
        <PvButton label="Вскрыть и списать" @click="commit(true)" />
      </template>
    </PvDialog>
  </section>
</template>

<style scoped>
.form { max-width: 640px; }
.field { display: flex; flex-direction: column; gap: 0.25rem; margin-bottom: 1rem; }
.field > span { font-weight: 600; font-size: 0.9rem; }
.row { display: flex; gap: 0.5rem; align-items: center; }
.result { display: flex; flex-direction: column; gap: 1rem; }
.stock { background: rgba(0,0,0,0.04); border-radius: 8px; padding: 0.6rem 0.8rem; margin-bottom: 1rem; display: flex; gap: 0.6rem; flex-wrap: wrap; align-items: center; }
.plan { margin-bottom: 1rem; }
.plan ul { margin: 0.4rem 0; padding-left: 1.25rem; }
.dt { padding: 0.5rem; border: 1px solid #d1d5db; border-radius: 6px; font: inherit; }
</style>
