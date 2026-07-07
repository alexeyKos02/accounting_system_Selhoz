<script setup lang="ts">
import { RouterLink } from 'vue-router'
import { useI18n } from 'vue-i18n'
const { t } = useI18n()
</script>

<template>
  <section class="page">
    <h1 class="page__title">404</h1>
    <p class="page__stub">{{ t('common.notFound') }}</p>
    <RouterLink to="/">{{ t('nav.dashboard') }}</RouterLink>
  </section>
</template>
