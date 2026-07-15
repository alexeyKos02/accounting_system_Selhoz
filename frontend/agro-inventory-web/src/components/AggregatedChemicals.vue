<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { allCompaniesApi } from '../api/catalog'
import type { AggregatedChemicalGroupDto } from '../api/types'
import { ApiError } from '../api/http'

// Общий режим «Все хозяйства» для химии (ТЗ §17): суммарно по каноническому препарату либо
// раздельно по хозяйствам. Данные приходят сгруппированными; раздельный режим — разворот позиций.
const groups = ref<AggregatedChemicalGroupDto[]>([])
const loading = ref(false)
const error = ref('')
const combined = ref(true) // true — «Объединить одинаковые препараты»; false — «Отдельно по хозяйствам»
const expanded = ref<Record<string, boolean>>({})

function fmt(n: number | undefined) {
  return (n ?? 0).toLocaleString('ru-RU', { maximumFractionDigits: 3 })
}

// Раздельный режим: каждая позиция хозяйства — своя строка.
const separateRows = computed(() =>
  groups.value.flatMap((g) =>
    (g.positions ?? []).map((p) => ({
      key: `${g.key}:${p.inventoryItemId}`,
      companyName: p.companyName,
      localName: p.localName,
      canonicalName: g.linked ? g.name : null,
      liters: p.totalLiters ?? 0,
    })),
  ).sort((a, b) => (a.companyName ?? '').localeCompare(b.companyName ?? '')))

async function load() {
  loading.value = true
  error.value = ''
  try {
    groups.value = await allCompaniesApi.chemicals()
  } catch (e) {
    error.value = e instanceof ApiError ? e.message : 'Не удалось загрузить остатки'
  } finally {
    loading.value = false
  }
}

function toggle(key: string | null | undefined) {
  const k = key ?? ''
  expanded.value[k] = !expanded.value[k]
}
function isOpen(key: string | null | undefined) {
  return expanded.value[key ?? '']
}

onMounted(load)
</script>

<template>
  <section class="page">
    <div class="head">
      <h1 class="page__title">Химия — все хозяйства</h1>
      <div class="modes">
        <PvButton :outlined="!combined" size="small" label="Объединить одинаковые" @click="combined = true" />
        <PvButton :outlined="combined" size="small" label="Отдельно по хозяйствам" @click="combined = false" />
      </div>
    </div>

    <PvMessage v-if="error" severity="error" :closable="false">{{ error }}</PvMessage>
    <div v-if="loading" class="loading"><PvProgressSpinner style="width:2.5rem;height:2.5rem" /></div>

    <!-- Суммарный режим (ТЗ §17): группировка по каноническому препарату -->
    <div v-else-if="combined" class="groups">
      <p v-if="!groups.length" class="muted">Нет остатков по доступным хозяйствам.</p>
      <div v-for="g in groups" :key="g.key ?? ''" class="group">
        <button class="group__head" @click="toggle(g.key)">
          <i class="pi" :class="isOpen(g.key) ? 'pi-chevron-down' : 'pi-chevron-right'" />
          <span class="group__name">{{ g.name }}</span>
          <PvTag v-if="!g.linked" value="без каталога" severity="secondary" />
          <span class="group__meta">{{ fmt(g.totalLiters) }} л · хозяйств {{ g.companiesCount }} · складов {{ g.warehousesCount }}</span>
        </button>
        <div v-if="isOpen(g.key)" class="group__body">
          <div v-for="p in g.positions" :key="p.inventoryItemId ?? ''" class="pos">
            <div class="pos__row">
              <span class="pos__company">{{ p.companyName }}</span>
              <span class="pos__local">«{{ p.localName }}»</span>
              <span class="pos__liters">{{ fmt(p.totalLiters) }} л</span>
            </div>
            <div class="pos__wh">
              <span v-for="w in p.warehouses" :key="w.warehouseId" class="chip">
                {{ w.warehouseNumber }}: {{ fmt(w.totalLiters) }} л
              </span>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Раздельный режим (ТЗ §17): каждая позиция хозяйства отдельной строкой -->
    <PvDataTable v-else :value="separateRows" data-key="key" class="mt">
      <PvColumn field="companyName" header="Хозяйство" />
      <PvColumn field="localName" header="Позиция хозяйства" />
      <PvColumn field="canonicalName" header="Каталожный препарат">
        <template #body="{ data }">
          <span v-if="data.canonicalName">{{ data.canonicalName }}</span>
          <span v-else class="muted">—</span>
        </template>
      </PvColumn>
      <PvColumn header="Остаток">
        <template #body="{ data }">{{ fmt(data.liters) }} л</template>
      </PvColumn>
    </PvDataTable>
  </section>
</template>

<style scoped>
.head { display: flex; justify-content: space-between; align-items: center; flex-wrap: wrap; gap: 0.75rem; margin-bottom: 1rem; }
.modes { display: flex; gap: 0.5rem; }
.loading { display: flex; justify-content: center; padding: 2rem; }
.muted { color: var(--p-text-muted-color, #6b7280); }
.mt { margin-top: 1rem; }
.groups { display: flex; flex-direction: column; gap: 0.5rem; }
.group { border: 1px solid var(--p-content-border-color, #e5e7eb); border-radius: 10px; overflow: hidden; }
.group__head {
  display: flex; align-items: center; gap: 0.6rem; width: 100%;
  padding: 0.75rem 1rem; background: transparent; border: none; cursor: pointer; text-align: left; color: inherit;
}
.group__head:hover { background: rgba(0,0,0,0.03); }
.group__name { font-weight: 600; }
.group__meta { margin-left: auto; color: var(--p-text-muted-color, #6b7280); font-size: 0.9rem; }
.group__body { padding: 0.25rem 1rem 0.75rem 2.2rem; display: flex; flex-direction: column; gap: 0.5rem; }
.pos__row { display: flex; gap: 0.6rem; align-items: baseline; }
.pos__company { font-weight: 600; }
.pos__local { color: var(--p-text-muted-color, #6b7280); }
.pos__liters { margin-left: auto; }
.pos__wh { display: flex; flex-wrap: wrap; gap: 0.35rem; margin-top: 0.2rem; }
.chip { font-size: 0.8rem; padding: 0.1rem 0.5rem; border-radius: 999px; background: rgba(59,130,246,0.1); }
</style>
