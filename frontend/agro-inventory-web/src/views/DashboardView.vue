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
  { label: 'Приход', icon: 'pi pi-plus-circle', to: '/income' },
  { label: 'Списание', icon: 'pi pi-minus-circle', to: '/outcome' },
  { label: 'Инвентаризация', icon: 'pi pi-check-square', to: '/inventory-check' },
  { label: 'Корректировка', icon: 'pi pi-sliders-h', to: '/corrections' },
]

onMounted(load)
</script>

<template>
  <section class="page">
    <h1 class="page__title">Дашборд</h1>

    <!-- Быстрые действия (ТЗ §22) -->
    <div class="quick">
      <PvButton v-for="q in quick" :key="q.to" :label="q.label" :icon="q.icon" outlined @click="router.push(q.to)" />
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
  </section>
</template>

<style scoped>
.quick { display: flex; gap: 0.5rem; flex-wrap: wrap; margin-bottom: 1.25rem; }
.stats { display: grid; grid-template-columns: repeat(auto-fit, minmax(140px, 1fr)); gap: 0.75rem; margin-bottom: 1.5rem; }
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
}
</style>
