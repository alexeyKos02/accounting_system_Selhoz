<script setup lang="ts">
import { useI18n } from 'vue-i18n'
const { t } = useI18n()
</script>

<template>
  <section class="page">
    <h1 class="page__title">{{ t('nav.settings') }}</h1>
    <p class="page__stub">Настройки (этап 7).</p>
  </section>
</template>
