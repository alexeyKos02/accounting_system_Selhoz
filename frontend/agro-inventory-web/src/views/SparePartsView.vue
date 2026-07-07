<script setup lang="ts">
import { useI18n } from 'vue-i18n'
const { t } = useI18n()
</script>

<template>
  <section class="page">
    <h1 class="page__title">{{ t('nav.spareParts') }}</h1>
    <p class="page__stub">{{ t('spareParts.comingSoon') }}</p>
  </section>
</template>
