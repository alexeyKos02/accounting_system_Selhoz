<script setup lang="ts">
import { useI18n } from 'vue-i18n'
const { t } = useI18n()
</script>

<template>
  <section class="page">
    <h1 class="page__title">{{ t('chemical.create') }}</h1>
    <p class="page__stub">Создание химии / химия с приходом, проверка дублей (этап 3).</p>
  </section>
</template>
