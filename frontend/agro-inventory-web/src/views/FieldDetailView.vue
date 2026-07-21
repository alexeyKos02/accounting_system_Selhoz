<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useToast } from 'primevue/usetoast'
import { fieldsApi, cropsApi } from '../api/reference'
import type { CropDto, FieldDto } from '../api/types'
import { unitLabel } from '../api/types'
import { fieldSeasonsApi } from '../api/fieldSeasons'
import type { FieldSeasonDto } from '../api/fieldSeasons'
import { treatmentsApi } from '../api/treatments'
import type { FieldTreatmentDto } from '../api/treatments'
import { ApiError } from '../api/http'

const route = useRoute()
const router = useRouter()
const toast = useToast()

const id = computed(() => String(route.params.id))
const field = ref<FieldDto | null>(null)
const crops = ref<CropDto[]>([])
const seasons = ref<FieldSeasonDto[]>([])
const treatments = ref<FieldTreatmentDto[]>([])
const loading = ref(false)
const savingSeason = ref(false)

const seasonYear = ref(new Date().getFullYear())
const seasonCropId = ref<string | null>(null)
const seasonName = ref('')
const seasonStartedAt = ref('')
const seasonFinishedAt = ref('')
const seasonComment = ref('')

const cropOptions = computed(() => crops.value.map((c) => ({ label: c.name, value: c.id })))

function fail(e: unknown, fallback: string) {
  toast.add({ severity: 'error', summary: 'Ошибка', detail: e instanceof ApiError ? e.message : fallback, life: 4500 })
}

function fmtNum(n?: number | null) {
  return (n ?? 0).toLocaleString('ru-RU', { maximumFractionDigits: 3 })
}

function fmtDate(iso?: string | null) {
  return iso ? new Date(iso).toLocaleDateString('ru-RU') : '—'
}

function dateToIso(value: string) {
  return value ? new Date(`${value}T00:00:00`).toISOString() : null
}

async function load() {
  loading.value = true
  try {
    ;[field.value, seasons.value, treatments.value, crops.value] = await Promise.all([
      fieldsApi.get(id.value),
      fieldSeasonsApi.list(id.value),
      treatmentsApi.list(id.value),
      cropsApi.list(),
    ])
  } catch (e) {
    fail(e, 'Не удалось загрузить поле')
  } finally {
    loading.value = false
  }
}

async function addSeason() {
  if (!seasonCropId.value) {
    toast.add({ severity: 'warn', summary: 'Выберите культуру', life: 2500 })
    return
  }
  savingSeason.value = true
  try {
    await fieldSeasonsApi.create({
      fieldId: id.value,
      cropId: seasonCropId.value,
      year: seasonYear.value,
      name: seasonName.value.trim() || null,
      startedAt: dateToIso(seasonStartedAt.value),
      finishedAt: dateToIso(seasonFinishedAt.value),
      comment: seasonComment.value.trim() || null,
    })
    seasonName.value = ''
    seasonStartedAt.value = ''
    seasonFinishedAt.value = ''
    seasonComment.value = ''
    toast.add({ severity: 'success', summary: 'Сезон добавлен', life: 2200 })
    seasons.value = await fieldSeasonsApi.list(id.value)
  } catch (e) {
    fail(e, 'Не удалось добавить сезон')
  } finally {
    savingSeason.value = false
  }
}

function goTreatment() {
  router.push({ name: 'field-treatments', query: { fieldId: id.value } })
}

onMounted(load)
</script>

<template>
  <section class="page">
    <PvButton label="К списку полей" icon="pi pi-arrow-left" text class="back" @click="router.push({ name: 'fields' })" />

    <div v-if="loading && !field" class="empty">Загрузка…</div>
    <template v-else-if="field">
      <div class="field-head">
        <div>
          <h1 class="page__title">Поле {{ field.number }}</h1>
          <div class="field-head__meta">
            <span>{{ field.areaHectares != null ? `${fmtNum(field.areaHectares)} га` : 'Площадь не указана' }}</span>
            <span>{{ field.currentCropName ? `Текущая культура: ${field.currentCropName}` : 'Текущая культура не указана' }}</span>
          </div>
        </div>
        <PvButton label="Добавить обработку" icon="pi pi-sparkles" @click="goTreatment" />
      </div>

      <div class="detail-grid">
        <section class="panel">
          <div class="panel__head">
            <h2>Сезоны</h2>
            <span>{{ seasons.length }}</span>
          </div>

          <div class="season-form">
            <PvInputText v-model.number="seasonYear" type="number" min="2000" max="2100" placeholder="Год" />
            <PvSelect
              v-model="seasonCropId"
              :options="cropOptions"
              option-label="label"
              option-value="value"
              filter
              placeholder="Культура"
            />
            <PvInputText v-model="seasonName" placeholder="Название сезона" />
            <input v-model="seasonStartedAt" class="date" type="date" />
            <input v-model="seasonFinishedAt" class="date" type="date" />
            <PvInputText v-model="seasonComment" placeholder="Комментарий" />
            <PvButton label="Добавить сезон" icon="pi pi-plus" :loading="savingSeason" @click="addSeason" />
          </div>

          <div class="season-list">
            <article v-for="season in seasons" :key="season.id!" class="season-card">
              <div class="season-card__main">
                <strong>{{ season.name || `Сезон ${season.year}` }}</strong>
                <PvTag :value="season.cropName" severity="success" />
              </div>
              <div class="season-card__meta">
                <span>{{ season.year }}</span>
                <span>{{ fmtDate(season.startedAt) }} — {{ fmtDate(season.finishedAt) }}</span>
              </div>
              <div v-if="season.comment" class="season-card__comment">{{ season.comment }}</div>
            </article>
            <div v-if="!seasons.length" class="empty">Сезонов пока нет</div>
          </div>
        </section>

        <section class="panel">
          <div class="panel__head">
            <h2>Обработки поля</h2>
            <span>{{ treatments.length }}</span>
          </div>
          <div class="treatments">
            <article v-for="item in treatments" :key="item.id!" class="treatment-card">
              <div class="treatment-card__top">
                <strong>{{ item.chemicalName }}</strong>
                <span>{{ fmtDate(item.treatedAt) }}</span>
              </div>
              <div class="treatment-card__chips">
                <span>{{ item.cropName }}</span>
                <span>{{ fmtNum(item.quantity) }} {{ unitLabel(item.measureUnit) }}</span>
                <span>Склад {{ item.warehouseNumber }}</span>
                <span v-if="item.ratePerHectare">Норма {{ fmtNum(item.ratePerHectare) }} {{ unitLabel(item.measureUnit) }}/га</span>
              </div>
              <div v-if="item.comment" class="season-card__comment">{{ item.comment }}</div>
            </article>
            <div v-if="!treatments.length" class="empty">Обработок пока нет</div>
          </div>
        </section>
      </div>
    </template>
  </section>
</template>

<style scoped>
.back { margin-bottom: 0.75rem; }
.field-head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 1rem;
}
.field-head__meta {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem 1rem;
  color: #6b7280;
}
.detail-grid {
  display: grid;
  grid-template-columns: minmax(0, 1fr) minmax(0, 1fr);
  gap: 1rem;
  margin-top: 1rem;
}
.panel {
  border: 1px solid #e5e7eb;
  border-radius: 8px;
  background: #fff;
  overflow: hidden;
}
.panel__head {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 1rem;
  padding: 1rem;
  border-bottom: 1px solid #e5e7eb;
}
.panel__head h2 {
  margin: 0;
  font-size: 1rem;
}
.panel__head span {
  color: #6b7280;
}
.season-form {
  display: grid;
  grid-template-columns: 7rem minmax(10rem, 1fr);
  gap: 0.5rem;
  padding: 1rem;
  border-bottom: 1px solid #e5e7eb;
}
.date {
  padding: 0.5rem;
  border: 1px solid var(--p-inputtext-border-color, #d1d5db);
  border-radius: 6px;
  font: inherit;
}
.season-list, .treatments {
  display: flex;
  flex-direction: column;
}
.season-card, .treatment-card {
  padding: 1rem;
  border-bottom: 1px solid #eef2f7;
}
.season-card:last-child, .treatment-card:last-child {
  border-bottom: 0;
}
.season-card__main, .treatment-card__top {
  display: flex;
  justify-content: space-between;
  gap: 1rem;
  align-items: center;
}
.season-card__meta, .treatment-card__top span {
  display: flex;
  gap: 0.75rem;
  color: #6b7280;
  margin-top: 0.35rem;
}
.season-card__comment {
  margin-top: 0.45rem;
  color: #4b5563;
}
.treatment-card__chips {
  display: flex;
  flex-wrap: wrap;
  gap: 0.4rem;
  margin-top: 0.6rem;
}
.treatment-card__chips span {
  padding: 0.25rem 0.5rem;
  border: 1px solid #e5e7eb;
  border-radius: 6px;
  color: #374151;
  background: #f9fafb;
}
.empty {
  padding: 1rem;
  color: #6b7280;
}
@media (max-width: 760px) {
  .field-head { flex-direction: column; }
  .field-head :deep(.p-button) { width: 100%; }
  .detail-grid { grid-template-columns: 1fr; }
  .season-form { grid-template-columns: 1fr; }
  .date { width: 100%; min-width: 0; max-width: 100%; box-sizing: border-box; -webkit-appearance: none; appearance: none; }
}
</style>
