<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { dashboardApi } from '../api/dashboard'
import type { DashboardDto, DashboardStockDto } from '../api/dashboard'
import { MovementType } from '../api/history'

const router = useRouter()
const data = ref<DashboardDto | null>(null)
const loading = ref(false)

async function load() {
  loading.value = true
  try {
    data.value = await dashboardApi.get()
  } finally {
    loading.value = false
  }
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
function openChemical(row: DashboardStockDto) {
  router.push({ name: 'chemical-detail', params: { id: row.chemicalId } })
}

const quick = [
  { label: 'Приход', icon: 'pi pi-plus-circle', to: '/income', color: 'green' },
  { label: 'Списание', icon: 'pi pi-minus-circle', to: '/outcome', color: 'orange' },
  { label: 'Инвентаризация', icon: 'pi pi-check-square', to: '/inventory-check', color: 'blue' },
  { label: 'Корректировка', icon: 'pi pi-sliders-h', to: '/corrections', color: 'violet' },
]

onMounted(load)
</script>

<template>
  <section class="page">
    <h1 class="page__title">Дашборд</h1>

    <!-- Быстрые действия (ТЗ §22): на телефоне — иконка сверху/текст снизу, на десктопе — кнопки-контуры -->
    <div class="quick">
      <button v-for="q in quick" :key="q.to" type="button" class="quick__card" :class="`quick__card--${q.color}`"
        :aria-label="q.label" @click="router.push(q.to)">
        <i class="pi" :class="q.icon" />
        <span>{{ q.label }}</span>
      </button>
    </div>

    <!-- Счётчики -->
    <div class="stats">
      <div class="stat">
        <div class="stat__value">{{ data?.activeChemicals ?? 0 }}</div>
        <div class="stat__label">Химии в работе</div>
      </div>
      <div class="stat">
        <div class="stat__value">{{ fmtNum(data?.totalLiters) }} л</div>
        <div class="stat__label">Всего на складах</div>
      </div>
      <div class="stat stat--warn" :class="{ 'stat--muted': !data?.lowCount }">
        <div class="stat__value">{{ data?.lowCount ?? 0 }}</div>
        <div class="stat__label">Малый остаток</div>
      </div>
      <div class="stat stat--danger" :class="{ 'stat--muted': !data?.emptyCount }">
        <div class="stat__value">{{ data?.emptyCount ?? 0 }}</div>
        <div class="stat__label">Закончилась</div>
      </div>
      <div class="stat">
        <div class="stat__value">{{ data?.warehouses ?? 0 }}</div>
        <div class="stat__label">Складов</div>
      </div>
    </div>

    <div class="columns">
      <!-- Закончилась -->
      <div class="block">
        <div class="block__head"><i class="pi pi-times-circle" /> Закончилась</div>
        <ul v-if="data?.empty?.length" class="list">
          <li v-for="c in data.empty" :key="c.chemicalId" @click="openChemical(c)">
            <span class="name">{{ c.name }}</span>
            <PvTag value="0 л" severity="danger" />
          </li>
        </ul>
        <div v-else class="empty">Всё в наличии</div>
      </div>

      <!-- Малый остаток -->
      <div class="block">
        <div class="block__head"><i class="pi pi-exclamation-triangle" /> Малый остаток
          <span v-if="data" class="thr">(&lt; {{ fmtNum(data.lowStockThresholdLiters) }} л)</span>
        </div>
        <ul v-if="data?.low?.length" class="list">
          <li v-for="c in data.low" :key="c.chemicalId" @click="openChemical(c)">
            <span class="name">{{ c.name }}</span>
            <PvTag :value="`${fmtNum(c.totalLiters)} л`" severity="warn" />
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
        <PvDataTable :value="data?.recentOperations ?? []" :loading="loading" data-key="id" size="small">
          <PvColumn header="Дата"><template #body="{ data: r }">{{ fmtDate(r.occurredAt) }}</template></PvColumn>
          <PvColumn header="Тип"><template #body="{ data: r }">
            <PvTag :value="typeLabel(r.movementType)" :severity="typeSeverity(r.movementType)" />
          </template></PvColumn>
          <PvColumn field="chemicalName" header="Химия" />
          <PvColumn header="Кол-во, л"><template #body="{ data: r }">{{ fmtNum(r.quantityLiters) }}</template></PvColumn>
          <PvColumn header="Склад"><template #body="{ data: r }">Склад {{ r.warehouseNumber }}</template></PvColumn>
          <template #empty><div class="empty">Операций пока нет</div></template>
        </PvDataTable>
      </div>

      <!-- Мобилка: карточки (как на странице «История») -->
      <div class="ops-cards">
        <div v-for="r in data?.recentOperations ?? []" :key="r.id!" class="ops-card">
          <div class="ops-card__row">
            <PvTag :value="typeLabel(r.movementType)" :severity="typeSeverity(r.movementType)" />
            <span class="ops-card__qty">{{ fmtNum(r.quantityLiters) }} л</span>
          </div>
          <div class="ops-card__name">{{ r.chemicalName }}</div>
          <div class="ops-card__meta">{{ fmtDate(r.occurredAt!) }} · Склад {{ r.warehouseNumber }}</div>
        </div>
        <div v-if="!(data?.recentOperations?.length)" class="empty">Операций пока нет</div>
      </div>
    </div>
  </section>
</template>

<style scoped>
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

/* Телефон: минимализм — иконка сверху (в цвете действия), подпись снизу, без рамок, линия-разделитель. */
@media (max-width: 768px) {
  .quick {
    display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 0.25rem;
    border-bottom: 1px solid var(--p-content-border-color, #e5e7eb);
    padding-bottom: 0.85rem; margin-bottom: 1.25rem;
  }
  .quick__card {
    flex-direction: column; gap: 0.35rem; padding: 0.5rem 0.15rem;
    min-width: 0; border: none; background: transparent; color: #6b7280; font-weight: 500;
  }
  .quick__card i { font-size: 1.7rem; color: var(--qc, #16a34a); }
  /* Длинные подписи («Инвентаризация», «Корректировка») переносятся, а не расширяют колонку. */
  .quick__card span { font-size: 0.68rem; line-height: 1.05; text-align: center; overflow-wrap: anywhere; }
  .quick__card:hover { background: transparent; }
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
