<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { dashboardApi } from '../api/dashboard'
import type { AllCompaniesDashboardDto, DashboardDto, DashboardStockDto } from '../api/dashboard'
import { allCompaniesApi } from '../api/catalog'
import type { AggregatedChemicalGroupDto } from '../api/types'
import { unitLabel } from '../api/types'
import { MovementType } from '../api/history'
import { useCompanyContextStore } from '../stores/companyContext'

const router = useRouter()
const ctx = useCompanyContextStore()
const data = ref<DashboardDto | null>(null)
const allData = ref<AllCompaniesDashboardDto | null>(null)
const loading = ref(false)
const quickOpen = ref(false)
const period = ref<'today' | 'week' | 'month' | 'quarter' | 'year' | 'custom'>('month')
const dateFrom = ref('')
const dateTo = ref('')

// История на дашборде: 10 записей на десктопе, 4 на мобилке.
const recentDesktop = computed(() => (data.value?.recentOperations ?? []).slice(0, 10))
const recentMobile = computed(() => (data.value?.recentOperations ?? []).slice(0, 4))
const allRecentDesktop = computed(() => (allData.value?.recentOperations ?? []).slice(0, 10))
const allRecentMobile = computed(() => (allData.value?.recentOperations ?? []).slice(0, 4))

// Список всей химии компании: на дашборде показываем первые 5.
const topChemicals = computed(() => (data.value?.chemicals ?? []).slice(0, 5))

// Химия по всем хозяйствам: агрегированные группы, на дашборде — первые 5 по алфавиту.
const chemicalGroups = ref<AggregatedChemicalGroupDto[]>([])
const expandedGroups = ref<Record<string, boolean>>({})
const topGroups = computed(() =>
  [...chemicalGroups.value]
    .sort((a, b) => (a.name ?? '').localeCompare(b.name ?? '', 'ru'))
    .slice(0, 5),
)
function toggleGroup(key?: string | null) {
  const k = key ?? ''
  expandedGroups.value[k] = !expandedGroups.value[k]
}
function isGroupOpen(key?: string | null) {
  return !!expandedGroups.value[key ?? '']
}
// Разворот группы: остаток препарата по каждому хозяйству (позиции хозяйства суммируются).
function perCompany(g: AggregatedChemicalGroupDto) {
  const map = new Map<string, { companyId: string; companyName: string; totalQuantity: number }>()
  for (const p of g.positions ?? []) {
    const id = p.companyId ?? ''
    const cur = map.get(id)
    if (cur) cur.totalQuantity += p.totalQuantity ?? 0
    else map.set(id, { companyId: id, companyName: p.companyName ?? '', totalQuantity: p.totalQuantity ?? 0 })
  }
  return [...map.values()].sort((a, b) => a.companyName.localeCompare(b.companyName, 'ru'))
}
async function loadChemicals() {
  chemicalGroups.value = await allCompaniesApi.chemicals()
}

async function load() {
  loading.value = true
  try {
    if (ctx.isAllCompaniesMode) allData.value = await dashboardApi.getAll(periodFilters())
    else data.value = await dashboardApi.get()
  } finally {
    loading.value = false
  }
}

function setPeriod(value: typeof period.value) {
  period.value = value
  const now = new Date()
  const start = new Date(now)
  const end = new Date(now)
  if (value === 'today') {
    start.setHours(0, 0, 0, 0)
    end.setHours(23, 59, 59, 999)
  } else if (value === 'week') {
    start.setDate(now.getDate() - 6)
    start.setHours(0, 0, 0, 0)
    end.setHours(23, 59, 59, 999)
  } else if (value === 'month') {
    start.setMonth(now.getMonth() - 1)
    start.setHours(0, 0, 0, 0)
    end.setHours(23, 59, 59, 999)
  } else if (value === 'quarter') {
    start.setMonth(now.getMonth() - 3)
    start.setHours(0, 0, 0, 0)
    end.setHours(23, 59, 59, 999)
  } else if (value === 'year') {
    start.setFullYear(now.getFullYear() - 1)
    start.setHours(0, 0, 0, 0)
    end.setHours(23, 59, 59, 999)
  } else {
    load()
    return
  }
  dateFrom.value = toDateInput(start)
  dateTo.value = toDateInput(end)
  load()
}

function periodFilters() {
  return {
    dateFrom: dateFrom.value ? new Date(`${dateFrom.value}T00:00:00`).toISOString() : undefined,
    dateTo: dateTo.value ? new Date(`${dateTo.value}T23:59:59.999`).toISOString() : undefined,
  }
}

function toDateInput(date: Date) {
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`
}

function fmtNum(n?: number) {
  return (n ?? 0).toLocaleString('ru-RU', { maximumFractionDigits: 3 })
}
function fmtDate(iso: string) {
  return new Date(iso).toLocaleString('ru-RU', { day: '2-digit', month: '2-digit', hour: '2-digit', minute: '2-digit' })
}
function typeLabel(t?: number) {
  return t === MovementType.Income ? 'Приход' : t === MovementType.Outcome ? 'Списание' : 'Корректировка'
}
function typeSeverity(t?: number) {
  return t === MovementType.Income ? 'success' : t === MovementType.Outcome ? 'warn' : 'info'
}
// StockStatus: InStock=0, Low=1, Empty=2
function stockSeverity(status?: number) {
  return status === 2 ? 'danger' : status === 1 ? 'warn' : 'success'
}
function openChemical(row: DashboardStockDto) {
  router.push({ name: 'chemical-detail', params: { id: row.chemicalId } })
}
function runQuickAction(to: string) {
  quickOpen.value = false
  router.push(to)
}

const quick = [
  { label: 'Приход', icon: 'pi pi-plus-circle', to: '/income', color: 'green' },
  { label: 'Списание', icon: 'pi pi-minus-circle', to: '/outcome', color: 'orange' },
  { label: 'Инвентаризация', icon: 'pi pi-check-square', to: '/inventory-check', color: 'blue' },
  { label: 'Корректировка', icon: 'pi pi-sliders-h', to: '/corrections', color: 'violet' },
]

onMounted(() => {
  if (ctx.isAllCompaniesMode) {
    setPeriod('month')
    loadChemicals()
  } else load()
})
</script>

<template>
  <section class="page">
    <div class="dashboard-head">
      <h1 class="page__title">{{ ctx.isAllCompaniesMode ? 'Дашборд — все хозяйства' : 'Дашборд' }}</h1>
    </div>

    <template v-if="ctx.isAllCompaniesMode">
      <div class="periods">
        <PvButton label="Сегодня" size="small" :outlined="period !== 'today'" @click="setPeriod('today')" />
        <PvButton label="Неделя" size="small" :outlined="period !== 'week'" @click="setPeriod('week')" />
        <PvButton label="Месяц" size="small" :outlined="period !== 'month'" @click="setPeriod('month')" />
        <PvButton label="Квартал" size="small" :outlined="period !== 'quarter'" @click="setPeriod('quarter')" />
        <PvButton label="Год" size="small" :outlined="period !== 'year'" @click="setPeriod('year')" />
        <PvButton label="Свой" size="small" :outlined="period !== 'custom'" @click="setPeriod('custom')" />
      </div>
      <div v-if="period === 'custom'" class="custom-period">
        <input v-model="dateFrom" class="date-input" type="date" @change="load" />
        <input v-model="dateTo" class="date-input" type="date" @change="load" />
      </div>

      <!-- Вся химия — все хозяйства (клик разворачивает остаток по каждому хозяйству) -->
      <div class="block">
        <div class="block__head"><i class="pi pi-box" /> Химия
          <RouterLink to="/chemicals" class="all">вся химия →</RouterLink>
        </div>
        <ul v-if="topGroups.length" class="list list--groups">
          <li v-for="g in topGroups" :key="g.key ?? ''" class="group-item">
            <button type="button" class="group-item__head" @click="toggleGroup(g.key)">
              <i class="pi" :class="isGroupOpen(g.key) ? 'pi-chevron-down' : 'pi-chevron-right'" />
              <span class="name">{{ g.name }}</span>
              <span class="group-item__meta">хозяйств {{ g.companiesCount }}</span>
              <PvTag :value="`${fmtNum(g.totalQuantity)} ${unitLabel(g.measureUnit)}`" severity="info" />
            </button>
            <div v-if="isGroupOpen(g.key)" class="group-item__body">
              <div v-for="p in perCompany(g)" :key="p.companyId" class="company-line">
                <span class="company-line__name">{{ p.companyName }}</span>
                <span class="company-line__qty">{{ fmtNum(p.totalQuantity) }} {{ unitLabel(g.measureUnit) }}</span>
              </div>
            </div>
          </li>
        </ul>
        <div v-else class="empty">Химия не заведена</div>
      </div>

      <div class="columns">
        <div class="block">
          <div class="block__head"><i class="pi pi-times-circle" /> Закончилась</div>
          <ul v-if="allData?.empty?.length" class="list">
            <li v-for="c in allData.empty" :key="`${c.companyId}:${c.chemicalId}`">
              <span class="name">{{ c.chemicalName }} · {{ c.companyName }}</span>
              <PvTag :value="`0 ${unitLabel(c.measureUnit)}`" severity="danger" />
            </li>
          </ul>
          <div v-else class="empty">Всё в наличии</div>
        </div>
        <div class="block">
          <div class="block__head"><i class="pi pi-exclamation-triangle" /> Малый остаток
            <span v-if="allData" class="thr">(порог: {{ fmtNum(allData.lowStockThresholdLiters) }} л / {{ fmtNum(allData.lowStockThresholdKg) }} кг)</span>
          </div>
          <ul v-if="allData?.low?.length" class="list">
            <li v-for="c in allData.low" :key="`${c.companyId}:${c.chemicalId}`">
              <span class="name">{{ c.chemicalName }} · {{ c.companyName }}</span>
              <PvTag :value="`${fmtNum(c.totalQuantity)} ${unitLabel(c.measureUnit)}`" severity="warn" />
            </li>
          </ul>
          <div v-else class="empty">Нет позиций на исходе</div>
        </div>
      </div>

      <div class="block">
        <div class="block__head"><i class="pi pi-clock" /> Последние операции</div>
        <div class="ops-desktop">
          <PvDataTable :value="allRecentDesktop" :loading="loading" data-key="id" size="small">
            <PvColumn header="Дата"><template #body="{ data: r }">{{ fmtDate(r.occurredAt) }}</template></PvColumn>
            <PvColumn header="Тип"><template #body="{ data: r }">
              <PvTag :value="typeLabel(r.movementType)" :severity="typeSeverity(r.movementType)" />
            </template></PvColumn>
            <PvColumn field="chemicalName" header="Химия" />
            <PvColumn header="Кол-во"><template #body="{ data: r }">{{ fmtNum(r.quantity) }} {{ unitLabel(r.measureUnit) }}</template></PvColumn>
            <PvColumn header="Склад"><template #body="{ data: r }">Склад {{ r.warehouseNumber }}</template></PvColumn>
            <template #empty><div class="empty">Операций пока нет</div></template>
          </PvDataTable>
        </div>
        <div class="ops-cards">
          <div v-for="r in allRecentMobile" :key="r.id!" class="ops-card">
            <div class="ops-card__row">
              <PvTag :value="typeLabel(r.movementType)" :severity="typeSeverity(r.movementType)" />
              <span class="ops-card__qty">{{ fmtNum(r.quantity) }} {{ unitLabel(r.measureUnit) }}</span>
            </div>
            <div class="ops-card__name">{{ r.chemicalName }}</div>
            <div class="ops-card__meta">{{ fmtDate(r.occurredAt!) }} · Склад {{ r.warehouseNumber }}</div>
          </div>
          <div v-if="!allRecentMobile.length" class="empty">Операций пока нет</div>
        </div>
      </div>
    </template>

    <template v-else>
    <!-- Быстрые действия: на десктопе кнопки остаются сверху, на телефоне используются в плавающем меню. -->
    <div class="quick">
      <button v-for="q in quick" :key="q.to" type="button" class="quick__card" :class="`quick__card--${q.color}`"
        :aria-label="q.label" @click="router.push(q.to)">
        <i class="pi" :class="q.icon" />
        <span>{{ q.label }}</span>
      </button>
    </div>

    <div class="quick-fab" :class="{ 'quick-fab--open': quickOpen }">
      <TransitionGroup name="quick-fab-item" tag="div" class="quick-fab__menu">
        <button
          v-for="q in quickOpen ? quick : []"
          :key="q.to"
          type="button"
          class="quick-fab__item"
          :class="`quick-fab__item--${q.color}`"
          @click="runQuickAction(q.to)"
        >
          <span class="quick-fab__icon"><i class="pi" :class="q.icon" /></span>
          <span class="quick-fab__label">{{ q.label }}</span>
        </button>
      </TransitionGroup>
      <button
        type="button"
        class="quick-fab__button"
        :aria-expanded="quickOpen"
        aria-label="Быстрые действия"
        @click="quickOpen = !quickOpen"
      >
        <i class="pi" :class="quickOpen ? 'pi-times' : 'pi-plus'" />
      </button>
    </div>

    <!-- Вся химия компании -->
    <div class="block">
      <div class="block__head"><i class="pi pi-box" /> Химия
        <RouterLink to="/chemicals" class="all">вся химия →</RouterLink>
      </div>
      <ul v-if="topChemicals.length" class="list">
        <li v-for="c in topChemicals" :key="c.chemicalId" @click="openChemical(c)">
          <span class="name">{{ c.name }}</span>
          <PvTag :value="`${fmtNum(c.totalQuantity)} ${unitLabel(c.measureUnit)}`" :severity="stockSeverity(c.status)" />
        </li>
      </ul>
      <div v-else class="empty">Химия не заведена</div>
    </div>

    <div class="columns">
      <!-- Закончилась -->
      <div class="block">
        <div class="block__head"><i class="pi pi-times-circle" /> Закончилась</div>
        <ul v-if="data?.empty?.length" class="list">
          <li v-for="c in data.empty" :key="c.chemicalId" @click="openChemical(c)">
            <span class="name">{{ c.name }}</span>
            <PvTag :value="`0 ${unitLabel(c.measureUnit)}`" severity="danger" />
          </li>
        </ul>
        <div v-else class="empty">Всё в наличии</div>
      </div>

      <!-- Малый остаток -->
      <div class="block">
        <div class="block__head"><i class="pi pi-exclamation-triangle" /> Малый остаток
          <span v-if="data" class="thr">(порог: {{ fmtNum(data.lowStockThresholdLiters) }} л / {{ fmtNum(data.lowStockThresholdKg) }} кг)</span>
        </div>
        <ul v-if="data?.low?.length" class="list">
          <li v-for="c in data.low" :key="c.chemicalId" @click="openChemical(c)">
            <span class="name">{{ c.name }}</span>
            <PvTag :value="`${fmtNum(c.totalQuantity)} ${unitLabel(c.measureUnit)}`" severity="warn" />
          </li>
        </ul>
        <div v-else class="empty">Нет позиций на исходе</div>
      </div>
    </div>

    <!-- Последние операции -->
    <div class="block">
      <div class="block__head"><i class="pi pi-clock" /> Последние операции
        <RouterLink to="/history" class="all">вся история →</RouterLink>
      </div>
      <!-- Десктоп: таблица -->
      <div class="ops-desktop">
        <PvDataTable :value="recentDesktop" :loading="loading" data-key="id" size="small">
          <PvColumn header="Дата"><template #body="{ data: r }">{{ fmtDate(r.occurredAt) }}</template></PvColumn>
          <PvColumn header="Тип"><template #body="{ data: r }">
            <PvTag :value="typeLabel(r.movementType)" :severity="typeSeverity(r.movementType)" />
          </template></PvColumn>
          <PvColumn field="chemicalName" header="Химия" />
          <PvColumn header="Кол-во"><template #body="{ data: r }">{{ fmtNum(r.quantity) }} {{ unitLabel(r.measureUnit) }}</template></PvColumn>
          <PvColumn header="Склад"><template #body="{ data: r }">Склад {{ r.warehouseNumber }}</template></PvColumn>
          <template #empty><div class="empty">Операций пока нет</div></template>
        </PvDataTable>
      </div>

      <!-- Мобилка: карточки (как на странице «История») -->
      <div class="ops-cards">
        <div v-for="r in recentMobile" :key="r.id!" class="ops-card">
          <div class="ops-card__row">
            <PvTag :value="typeLabel(r.movementType)" :severity="typeSeverity(r.movementType)" />
            <span class="ops-card__qty">{{ fmtNum(r.quantity) }} {{ unitLabel(r.measureUnit) }}</span>
          </div>
          <div class="ops-card__name">{{ r.chemicalName }}</div>
          <div class="ops-card__meta">{{ fmtDate(r.occurredAt!) }} · Склад {{ r.warehouseNumber }}</div>
        </div>
        <div v-if="!recentMobile.length" class="empty">Операций пока нет</div>
      </div>
    </div>
    </template>
  </section>
</template>

<style scoped>
.dashboard-head { display: flex; justify-content: space-between; align-items: center; gap: 1rem; }
.periods { display: flex; flex-wrap: wrap; gap: 0.4rem; margin-bottom: 0.75rem; }
.custom-period { display: flex; gap: 0.5rem; flex-wrap: wrap; margin-bottom: 1rem; }
.date-input {
  min-height: 2.35rem;
  padding: 0.5rem;
  border: 1px solid var(--p-inputtext-border-color, #d1d5db);
  border-radius: 6px;
  font: inherit;
}
/* Десктоп: зелёные кнопки-контуры «иконка + текст» в ряд. */
.quick { display: flex; flex-wrap: wrap; gap: 0.5rem; margin-bottom: 1.5rem; }
.quick__card {
  display: inline-flex; align-items: center; gap: 0.5rem;
  padding: 0.6rem 1rem; border-radius: 8px;
  border: 1px solid #16a34a; background: transparent; color: #16a34a;
  cursor: pointer; font: inherit; font-weight: 600;
  transition: background 0.15s ease, transform 0.08s ease;
}
.quick__card i { font-size: 1.1rem; color: #16a34a; }
.quick__card:hover { background: rgba(22, 163, 74, 0.06); }
.quick__card:active { transform: scale(0.98); }
.quick__card--green { --qc: #16a34a; }
.quick__card--orange { --qc: #ea580c; }
.quick__card--blue { --qc: #2563eb; }
.quick__card--violet { --qc: #7c3aed; }
.quick-fab { display: none; }

@media (max-width: 768px) {
  .dashboard-head { align-items: flex-start; flex-direction: column; }
  .periods { overflow-x: auto; flex-wrap: nowrap; padding-bottom: 0.2rem; }
  .date-input { width: 100%; min-width: 0; max-width: 100%; box-sizing: border-box; }
  .quick { display: none; }
  .quick-fab {
    display: flex;
    position: fixed;
    right: 1rem;
    bottom: calc(4.95rem + env(safe-area-inset-bottom));
    z-index: 30;
    flex-direction: column;
    align-items: flex-end;
    gap: 0.75rem;
    pointer-events: none;
  }
  .quick-fab__menu {
    display: flex;
    flex-direction: column;
    align-items: flex-end;
    gap: 0.45rem;
    max-width: calc(100vw - 2rem);
  }
  .quick-fab__item {
    --quick-color: #16a34a;
    --quick-soft: #dcfce7;
    display: inline-flex;
    align-items: center;
    justify-content: flex-end;
    gap: 0.55rem;
    min-height: 2.65rem;
    max-width: calc(100vw - 2rem);
    padding: 0.45rem 0.65rem 0.45rem 0.8rem;
    border: 1px solid var(--p-content-border-color, #e5e7eb);
    border-radius: 999px;
    background: var(--p-content-background, #fff);
    color: #374151;
    box-shadow: 0 8px 22px rgba(15, 23, 42, 0.12);
    cursor: pointer;
    font: inherit;
    font-weight: 600;
    pointer-events: auto;
    -webkit-tap-highlight-color: transparent;
    transition: border-color 0.15s ease, transform 0.12s ease;
  }
  .quick-fab__item--green { --quick-color: #16a34a; --quick-soft: #dcfce7; }
  .quick-fab__item--orange { --quick-color: #ea580c; --quick-soft: #ffedd5; }
  .quick-fab__item--blue { --quick-color: #2563eb; --quick-soft: #dbeafe; }
  .quick-fab__item--violet { --quick-color: #7c3aed; --quick-soft: #ede9fe; }
  .quick-fab__item:active { transform: translateY(1px) scale(0.98); }
  .quick-fab__icon {
    display: grid;
    place-items: center;
    width: 1.85rem;
    height: 1.85rem;
    border-radius: 999px;
    background: var(--quick-soft);
    color: var(--quick-color);
    flex: 0 0 auto;
  }
  .quick-fab__icon i { font-size: 1rem; }
  .quick-fab__label {
    min-width: 0;
    line-height: 1.15;
    overflow-wrap: anywhere;
  }
  .quick-fab__button {
    display: grid;
    place-items: center;
    width: 3.35rem;
    height: 3.35rem;
    border: none;
    border-radius: 999px;
    background: #16a34a;
    color: #fff;
    box-shadow: 0 10px 26px rgba(22, 163, 74, 0.28);
    cursor: pointer;
    font: inherit;
    pointer-events: auto;
    -webkit-tap-highlight-color: transparent;
    transition: background 0.15s ease, box-shadow 0.15s ease, transform 0.18s ease;
  }
  .quick-fab__button i {
    font-size: 1.25rem;
    transition: transform 0.18s ease;
  }
  .quick-fab--open .quick-fab__button {
    background: #15803d;
    box-shadow: 0 12px 28px rgba(21, 128, 61, 0.3);
  }
  .quick-fab--open .quick-fab__button i { transform: rotate(90deg); }
  .quick-fab-item-enter-active {
    transition: opacity 0.18s ease, transform 0.18s ease;
  }
  .quick-fab-item-leave-active {
    transition: opacity 0.13s ease, transform 0.13s ease;
  }
  .quick-fab-item-enter-from,
  .quick-fab-item-leave-to {
    opacity: 0;
    transform: translateY(0.7rem) scale(0.94);
  }
  .quick-fab-item-enter-to,
  .quick-fab-item-leave-from {
    opacity: 1;
    transform: translateY(0) scale(1);
  }
}
.stats { display: grid; grid-template-columns: repeat(auto-fit, minmax(140px, 1fr)); gap: 0.75rem; margin-bottom: 1.5rem; }
/* Последние операции: на десктопе таблица, на мобилке карточки (карточки скрыты по умолчанию). */
.ops-cards { display: none; }
.stat {
  border: 1px solid var(--p-content-border-color, #e5e7eb); border-radius: 12px;
  padding: 0.9rem 1rem; background: var(--p-content-background, #fff);
}
.stat__value { font-size: 1.6rem; font-weight: 700; line-height: 1.1; }
.stat__label { font-size: 0.85rem; color: #6b7280; margin-top: 0.25rem; }
.stat--warn:not(.stat--muted) .stat__value { color: #d97706; }
.stat--danger:not(.stat--muted) .stat__value { color: #dc2626; }
.columns { display: grid; grid-template-columns: 1fr 1fr; gap: 1rem; margin-bottom: 1rem; }
.block {
  border: 1px solid var(--p-content-border-color, #e5e7eb); border-radius: 12px;
  padding: 1rem; margin-bottom: 1rem; background: var(--p-content-background, #fff);
}
.block__head { display: flex; align-items: center; gap: 0.5rem; font-weight: 600; margin-bottom: 0.75rem; }
.block__head .thr { font-weight: 400; color: #6b7280; font-size: 0.85rem; }
.block__head .all { margin-left: auto; font-size: 0.85rem; text-decoration: none; }
.list { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; gap: 0.35rem; }
.list li {
  display: flex; align-items: center; justify-content: space-between; gap: 0.5rem;
  padding: 0.4rem 0.5rem; border-radius: 8px; cursor: pointer;
}
.list li:hover { background: rgba(0, 0, 0, 0.04); }
.list .name { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.list--groups { gap: 0.25rem; }
.group-item { border-radius: 8px; overflow: hidden; }
.group-item__head {
  display: flex; align-items: center; gap: 0.5rem; width: 100%;
  padding: 0.45rem 0.5rem; border: none; background: transparent; cursor: pointer;
  font: inherit; color: inherit; text-align: left; border-radius: 8px;
}
.group-item__head:hover { background: rgba(0, 0, 0, 0.04); }
.group-item__head .name { flex: 1 1 auto; font-weight: 500; }
.group-item__meta { color: #6b7280; font-size: 0.8rem; white-space: nowrap; }
.group-item__body {
  display: flex; flex-direction: column; gap: 0.15rem;
  padding: 0.15rem 0.5rem 0.5rem 1.85rem;
}
.company-line { display: flex; align-items: center; justify-content: space-between; gap: 0.5rem; padding: 0.2rem 0; }
.company-line__name { color: #374151; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.company-line__qty { font-weight: 600; color: #111827; white-space: nowrap; }
.empty { padding: 0.75rem 0.25rem; color: #6b7280; }
@media (max-width: 768px) {
  .columns { grid-template-columns: 1fr; }
  /* На узком экране — ровно 2 колонки, разрешаем сжатие ниже 140px, чтобы не выходить за экран. */
  .stats { grid-template-columns: repeat(2, minmax(0, 1fr)); }
  .block { min-width: 0; }
  .block__head { flex-wrap: wrap; }
  /* Таблицу прячем, показываем карточки — без горизонтального переполнения. */
  .ops-desktop { display: none; }
  .ops-cards { display: flex; flex-direction: column; gap: 0.6rem; }
  .ops-card {
    display: flex; flex-direction: column; gap: 0.3rem;
    padding: 0.7rem 0.75rem; border: 1px solid var(--p-content-border-color, #e5e7eb); border-radius: 10px;
  }
  .ops-card__row { display: flex; align-items: center; justify-content: space-between; gap: 0.5rem; }
  .ops-card__qty { font-weight: 700; color: #111827; }
  .ops-card__name { font-weight: 600; color: #374151; }
  .ops-card__meta { color: #6b7280; font-size: 0.85rem; }
}
</style>
