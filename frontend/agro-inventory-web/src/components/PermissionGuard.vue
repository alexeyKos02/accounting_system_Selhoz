<script setup lang="ts">
import { computed } from 'vue'
import { useCompanyContextStore } from '../stores/companyContext'

// Скрывает содержимое, если у пользователя нет нужного права в текущем хозяйстве (ТЗ §5).
// Итоговая проверка прав — всегда на backend; это только UX-скрытие кнопок.
const props = defineProps<{ permission: string }>()
const ctx = useCompanyContextStore()
const allowed = computed(() => ctx.has(props.permission))
</script>

<template>
  <slot v-if="allowed" />
</template>
