<script setup lang="ts">
import { useI18n } from 'vue-i18n'
const { t } = useI18n()
</script>

<template>
  <section class="page">
    <h1 class="page__title">{{ t('nav.warehouses') }}</h1>
    <p class="page__stub">Склады (этап 3).</p>
  </section>
</template>
