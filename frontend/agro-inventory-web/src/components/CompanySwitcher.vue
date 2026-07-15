<script setup lang="ts">
import { computed } from 'vue'
import { useCompanyContextStore } from '../stores/companyContext'

// Переключатель хозяйств (ТЗ §15). Список — только доступные пользователю хозяйства.
// Выбор сохраняется и отправляется в заголовке X-Company-Id. Перезапрос данных обеспечивает
// :key на RouterView в AppLayout (текущий экран перемонтируется при смене хозяйства).
const ctx = useCompanyContextStore()

// «Все хозяйства» (value = null) — общий режим просмотра (ТЗ §15, §17).
const options = computed(() => [
  { label: 'Все хозяйства', value: null as string | null },
  ...ctx.availableCompanies.map((c) => ({ label: c.name ?? '—', value: c.id as string | null })),
])

const selected = computed({
  get: () => ctx.selectedCompanyId,
  set: (value: string | null) => ctx.selectCompany(value),
})
</script>

<template>
  <PvSelect
    v-if="options.length > 0"
    v-model="selected"
    :options="options"
    option-label="label"
    option-value="value"
    class="company-switcher"
    placeholder="Хозяйство"
  />
</template>

<style scoped>
.company-switcher { min-width: 12rem; }
</style>
