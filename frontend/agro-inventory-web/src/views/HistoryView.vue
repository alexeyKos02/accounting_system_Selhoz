<script setup lang="ts">
import { useI18n } from 'vue-i18n'
const { t } = useI18n()
</script>

<template>
  <section class="page">
    <h1 class="page__title">{{ t('nav.history') }}</h1>
    <p class="page__stub">История операций — фильтры (этап 5).</p>
  </section>
</template>
