<script setup lang="ts">
import { useI18n } from 'vue-i18n'
const { t } = useI18n()
</script>

<template>
  <section class="page">
    <h1 class="page__title">{{ t('nav.dashboard') }}</h1>
    <p class="page__stub">Дашборд — блоки «Закончилась»/«Малый остаток», последние операции, быстрые кнопки (этап 6).</p>
  </section>
</template>
