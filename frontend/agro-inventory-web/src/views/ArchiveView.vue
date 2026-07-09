<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useToast } from 'primevue/usetoast'
import { chemicalsApi } from '../api/chemicals'
import type { ArchivedChemicalDto } from '../api/types'
import { chemicalTypeLabels } from '../api/types'
import { ApiError } from '../api/http'

const router = useRouter()
const toast = useToast()
const items = ref<ArchivedChemicalDto[]>([])
const loading = ref(false)

async function load() {
  loading.value = true
  try {
    items.value = await chemicalsApi.archived()
  } finally {
    loading.value = false
  }
}

async function restore(item: ArchivedChemicalDto) {
  try {
    await chemicalsApi.restore(item.id!)
    toast.add({ severity: 'success', summary: 'Восстановлено', life: 2000 })
    await load()
  } catch (e) {
    const msg = e instanceof ApiError ? e.message : 'Не удалось восстановить'
    toast.add({ severity: 'error', summary: 'Ошибка', detail: msg, life: 4000 })
  }
}

function cropsLabel(item: ArchivedChemicalDto): string {
  return (item.crops ?? []).map((c) => c.name).join(', ')
}

onMounted(load)
</script>

<template>
  <section class="page">
    <h1 class="page__title">Архив</h1>

    <PvDataTable :value="items" :loading="loading" data-key="id">
      <PvColumn field="name" header="Название">
        <template #body="{ data }">
          <a href="#" @click.prevent="router.push({ name: 'chemical-detail', params: { id: data.id } })">{{ data.name }}</a>
        </template>
      </PvColumn>
      <PvColumn header="Тип">
        <template #body="{ data }">{{ data.type ? chemicalTypeLabels[data.type] : '—' }}</template>
      </PvColumn>
      <PvColumn field="manufacturer" header="Производитель" />
      <PvColumn header="Культуры"><template #body="{ data }">{{ cropsLabel(data) }}</template></PvColumn>
      <PvColumn header="Остаток">
        <template #body="{ data }">{{ (data.totalLiters ?? 0).toLocaleString('ru-RU') }} л</template>
      </PvColumn>
      <PvColumn header="Архивирована">
        <template #body="{ data }">{{ new Date(data.archivedAt).toLocaleDateString('ru-RU') }}</template>
      </PvColumn>
      <PvColumn header="" style="width: 10rem">
        <template #body="{ data }">
          <PvButton label="Восстановить" icon="pi pi-undo" size="small" @click="restore(data)" />
        </template>
      </PvColumn>
      <template #empty><div class="empty">Архив пуст</div></template>
    </PvDataTable>
  </section>
</template>

<style scoped>
.empty { padding: 1rem; color: #6b7280; }
</style>
