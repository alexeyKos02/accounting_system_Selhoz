<script setup lang="ts">
import { useI18n } from 'vue-i18n'
const { t } = useI18n()
</script>

<template>
  <section class="page">
    <h1 class="page__title">{{ t('nav.auditLog') }}</h1>
    <p class="page__stub">Audit log (этап 5).</p>
  </section>
</template>
