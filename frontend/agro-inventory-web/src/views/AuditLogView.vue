<script setup lang="ts">
import { onMounted, ref, watch } from 'vue'
import { auditApi, AuditAction } from '../api/history'
import type { AuditLogDto, AuditFilters } from '../api/history'

const items = ref<AuditLogDto[]>([])
const loading = ref(false)
const filters = ref<AuditFilters>({})
let timer: ReturnType<typeof setTimeout> | undefined

const actionOptions = [
  { label: 'Создание', value: AuditAction.Create },
  { label: 'Изменение', value: AuditAction.Update },
  { label: 'Удаление', value: AuditAction.Delete },
  { label: 'Восстановление', value: AuditAction.Restore },
  { label: 'Архивирование', value: AuditAction.Archive },
  { label: 'Объединение', value: AuditAction.Merge },
]
const entityOptions = [
  { label: 'Химия', value: 'InventoryItem' },
  { label: 'Операция', value: 'InventoryMovement' },
  { label: 'Остаток', value: 'ChemicalStockBalance' },
]

function actionLabel(a?: number) {
  return actionOptions.find((o) => o.value === a)?.label ?? String(a)
}
function entityLabel(e?: string) {
  return entityOptions.find((o) => o.value === e)?.label ?? e
}
function fmtDate(iso: string) { return new Date(iso).toLocaleString('ru-RU') }

async function load() {
  loading.value = true
  try {
    items.value = await auditApi.list(filters.value)
  } finally {
    loading.value = false
  }
}

watch(filters, () => { clearTimeout(timer); timer = setTimeout(load, 300) }, { deep: true })

onMounted(load)
</script>

<template>
  <section class="page">
    <h1 class="page__title">Журнал изменений</h1>

    <div class="filters">
      <PvSelect v-model="filters.action" :options="actionOptions" option-label="label" option-value="value" show-clear placeholder="Действие" />
      <PvSelect v-model="filters.entityType" :options="entityOptions" option-label="label" option-value="value" show-clear placeholder="Сущность" />
    </div>

    <PvDataTable :value="items" :loading="loading" data-key="id" class="mt">
      <PvColumn header="Дата"><template #body="{ data }">{{ fmtDate(data.createdAt) }}</template></PvColumn>
      <PvColumn header="Хозяйство"><template #body="{ data }">{{ data.companyName ?? '—' }}</template></PvColumn>
      <PvColumn header="Действие"><template #body="{ data }"><PvTag :value="actionLabel(data.action)" /></template></PvColumn>
      <PvColumn header="Сущность"><template #body="{ data }">{{ entityLabel(data.entityType) }}</template></PvColumn>
      <PvColumn field="userName" header="Пользователь" />
      <PvColumn header="Изменения"><template #body="{ data }">
        <details v-if="data.oldValues || data.newValues">
          <summary>показать</summary>
          <pre class="json">старое: {{ data.oldValues || '—' }}
новое: {{ data.newValues || '—' }}</pre>
        </details>
      </template></PvColumn>
      <template #empty><div class="empty">Записей нет</div></template>
    </PvDataTable>
  </section>
</template>

<style scoped>
.filters { display: flex; gap: 0.5rem; flex-wrap: wrap; }
.mt { margin-top: 1rem; }
.empty { padding: 1rem; color: #6b7280; }
.json { white-space: pre-wrap; word-break: break-all; font-size: 0.8rem; max-width: 32rem; }
</style>
