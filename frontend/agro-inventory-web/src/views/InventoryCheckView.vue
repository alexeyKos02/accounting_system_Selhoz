<script setup lang="ts">
import { onMounted, ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useToast } from 'primevue/usetoast'
import { inventoryApi, CheckOutcome } from '../api/inventory'
import type { InventoryCheckLineDto, InventoryCheckResultDto } from '../api/inventory'
import { useReference } from '../composables/useReference'
import { nowLocalInput, localToIso } from '../utils/datetime'
import { ApiError } from '../api/http'

const router = useRouter()
const toast = useToast()
const ref_ = useReference()

interface Row extends InventoryCheckLineDto {
  actual: number | null
}

const warehouseId = ref<string | null>(null)
const warehouseNumber = ref<string>('')
const rows = ref<Row[]>([])
const loadingSheet = ref(false)
const loaded = ref(false)
const comment = ref('')
const occurredAt = ref(nowLocalInput())
const saving = ref(false)
const result = ref<InventoryCheckResultDto | null>(null)

function fmtNum(n?: number) {
  return (n ?? 0).toLocaleString('ru-RU', { maximumFractionDigits: 3 })
}

// Смена склада: сбрасываем итог прошлой инвентаризации и грузим новую ведомость.
function onWarehouseChange() {
  result.value = null
  loadSheet()
}

async function loadSheet() {
  if (!warehouseId.value) return
  loadingSheet.value = true
  try {
    const sheet = await inventoryApi.checkSheet(warehouseId.value)
    warehouseNumber.value = sheet.warehouseNumber ?? ''
    rows.value = (sheet.lines ?? []).map((l) => ({ ...l, actual: l.currentTotalLiters ?? 0 }))
    loaded.value = true
  } catch (e) {
    fail(e, 'Не удалось загрузить ведомость')
  } finally {
    loadingSheet.value = false
  }
}

function delta(r: Row): number | null {
  if (r.actual == null) return null
  return Number((r.actual - (r.currentTotalLiters ?? 0)).toFixed(3))
}

// Строки, где введённый факт отличается от текущего остатка.
const changed = computed(() =>
  rows.value.filter((r) => r.actual != null && (delta(r) ?? 0) !== 0),
)

function fail(e: unknown, fb: string) {
  toast.add({ severity: 'error', summary: 'Ошибка', detail: e instanceof ApiError ? e.message : fb, life: 4000 })
}

async function submit() {
  if (changed.value.length === 0) {
    toast.add({ severity: 'info', summary: 'Изменений нет', detail: 'Фактические остатки совпадают с учётными.', life: 3000 })
    return
  }
  if (changed.value.some((r) => (r.actual ?? 0) < 0)) {
    toast.add({ severity: 'warn', summary: 'Отрицательный остаток недопустим', life: 3000 })
    return
  }
  saving.value = true
  try {
    result.value = await inventoryApi.applyCheck({
      warehouseId: warehouseId.value!,
      entries: changed.value.map((r) => ({ chemicalId: r.chemicalId!, actualTotalLiters: r.actual! })),
      occurredAt: localToIso(occurredAt.value),
      comment: comment.value.trim() || null,
    })
    toast.add({ severity: 'success', summary: 'Инвентаризация проведена', life: 2500 })
    await loadSheet() // перечитываем остатки после применения
  } catch (e) {
    fail(e, 'Не удалось провести инвентаризацию')
  } finally {
    saving.value = false
  }
}

const needsDetailed = computed(() =>
  (result.value?.lines ?? []).filter((l) => l.outcome === CheckOutcome.NeedsDetailed),
)

function fixDetailed(chemicalId: string) {
  router.push({ name: 'corrections', query: { chemicalId } })
}

onMounted(ref_.load)
</script>

<template>
  <section class="page">
    <h1 class="page__title">Инвентаризация склада</h1>

    <div class="controls">
      <label class="field"><span>Склад *</span>
        <PvSelect v-model="warehouseId" :options="ref_.warehouseOptions.value" option-label="label" option-value="value"
          filter placeholder="Выберите склад" @change="onWarehouseChange" />
      </label>
      <PvButton v-if="warehouseId" label="Обновить ведомость" icon="pi pi-refresh" outlined :loading="loadingSheet" @click="loadSheet" />
    </div>

    <!-- Итог применения -->
    <div v-if="result" class="result">
      <PvMessage severity="success" :closable="false">
        Применено: {{ result.appliedCount }} · Без изменений: {{ result.unchangedCount }} · Требуют разбора: {{ result.needsDetailedCount }}
      </PvMessage>
      <div v-if="needsDetailed.length" class="detailed">
        <b>Не удалось применить автоматически</b> — убыль больше наливного остатка, нужна детальная инвентаризация:
        <ul>
          <li v-for="l in needsDetailed" :key="l.chemicalId">
            {{ l.chemicalName }}
            <PvButton label="Разобрать" size="small" text @click="fixDetailed(l.chemicalId!)" />
          </li>
        </ul>
      </div>
    </div>

    <template v-if="loaded">
      <div class="hint">
        Склад {{ warehouseNumber }} — впишите фактический остаток в литрах. Изменённые строки применятся корректировкой.
      </div>

      <PvDataTable :value="rows" :loading="loadingSheet" data-key="chemicalId" size="small" class="mt">
        <PvColumn field="chemicalName" header="Химия" />
        <PvColumn header="Учётный остаток">
          <template #body="{ data: r }">
            <div>{{ fmtNum(r.currentTotalLiters) }} л</div>
            <div class="sub">наливом {{ fmtNum(r.looseLiters) }} л<span v-if="r.fullPackages"> · {{ r.fullPackages }} упак.</span><span v-if="r.openedPackages"> · {{ r.openedPackages }} вскр.</span></div>
          </template>
        </PvColumn>
        <PvColumn header="Факт, л" style="width: 9rem">
          <template #body="{ data: r }">
            <PvInputText v-model.number="r.actual" type="number" min="0" class="actual" />
          </template>
        </PvColumn>
        <PvColumn header="Разница" style="width: 8rem">
          <template #body="{ data: r }">
            <span v-if="delta(r) !== null && delta(r) !== 0" :class="(delta(r) ?? 0) > 0 ? 'pos' : 'neg'">
              {{ (delta(r) ?? 0) > 0 ? '+' : '' }}{{ fmtNum(delta(r)!) }} л
            </span>
            <span v-else class="muted">—</span>
          </template>
        </PvColumn>
        <template #empty><div class="empty">На складе нет остатков химии</div></template>
      </PvDataTable>

      <div v-if="rows.length" class="footer">
        <label class="field grow"><span>Комментарий</span>
          <PvInputText v-model="comment" placeholder="например: плановая инвентаризация" />
        </label>
        <label class="field"><span>Дата и время</span>
          <input class="dt" type="datetime-local" v-model="occurredAt" />
        </label>
      </div>
      <div v-if="rows.length" class="actions">
        <PvButton :label="`Провести инвентаризацию (${changed.length})`" icon="pi pi-check"
          :loading="saving" :disabled="changed.length === 0" @click="submit" />
      </div>
    </template>
  </section>
</template>

<style scoped>
.controls { display: flex; gap: 0.75rem; align-items: flex-end; flex-wrap: wrap; margin-bottom: 1rem; }
.field { display: flex; flex-direction: column; gap: 0.25rem; }
.field > span { font-weight: 600; font-size: 0.9rem; }
.field.grow { flex: 1; min-width: 220px; }
.hint { color: #6b7280; margin-bottom: 0.5rem; }
.mt { margin-top: 0.5rem; }
.sub { font-size: 0.8rem; color: #6b7280; }
.actual { width: 100%; }
.pos { color: #16a34a; font-weight: 600; }
.neg { color: #dc2626; font-weight: 600; }
.muted { color: #9ca3af; }
.empty { padding: 1rem; color: #6b7280; }
.footer { display: flex; gap: 0.75rem; align-items: flex-end; flex-wrap: wrap; margin-top: 1rem; }
.dt { padding: 0.5rem; border: 1px solid #d1d5db; border-radius: 6px; font: inherit; }
.actions { margin-top: 1rem; }
.result { margin-bottom: 1rem; display: flex; flex-direction: column; gap: 0.5rem; }
.detailed { border: 1px solid #fcd34d; background: rgba(252, 211, 77, 0.12); border-radius: 8px; padding: 0.75rem; }
.detailed ul { margin: 0.35rem 0 0; padding-left: 1.1rem; }
.detailed li { display: flex; align-items: center; gap: 0.5rem; margin-bottom: 0.2rem; }
</style>
