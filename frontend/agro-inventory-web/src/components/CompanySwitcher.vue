<script setup lang="ts">
import { computed } from 'vue'
import { useCompanyContextStore } from '../stores/companyContext'

// Переключатель хозяйств (ТЗ §15). Список — только доступные пользователю хозяйства.
// Выбор сохраняется и отправляется в заголовке X-Company-Id. Перезапрос данных обеспечивает
// :key на RouterView в AppLayout (текущий экран перемонтируется при смене хозяйства).
const ctx = useCompanyContextStore()

const options = computed(() =>
  ctx.availableCompanies.map((c) => ({ label: c.name ?? '—', value: c.id })))

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
