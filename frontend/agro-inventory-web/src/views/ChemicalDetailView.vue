<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useToast } from 'primevue/usetoast'
import { chemicalsApi } from '../api/chemicals'
import { cropsApi } from '../api/reference'
import type { ChemicalDetailDto, ChemicalTypeValue, CropDto } from '../api/types'
import { ItemStatus, UnitType, chemicalTypeLabels, chemicalTypeOptions } from '../api/types'
import { ApiError } from '../api/http'

const route = useRoute()
const router = useRouter()
const toast = useToast()

const id = computed(() => route.params.id as string)
const chem = ref<ChemicalDetailDto | null>(null)
const loading = ref(false)
const expanded = ref(window.innerWidth > 768) // ТЗ §15.2: на компьютере раскрыто, на телефоне закрыто

const crops = ref<CropDto[]>([])
const editDialog = ref(false)
const edit = ref({ name: '', type: null as ChemicalTypeValue | null, manufacturer: '', comment: '', cropIds: [] as string[] })

const typeLabel = computed(() =>
  chem.value?.type != null ? chemicalTypeLabels[chem.value.type] : null)

const archiveDialog = ref(false)
const archiveWord = ref('')

const cropOptions = computed(() => crops.value.map((c) => ({ label: c.name, value: c.id })))
const isArchived = computed(() => chem.value?.status === ItemStatus.Archived)
const isMerged = computed(() => chem.value?.status === ItemStatus.Merged)

function unitLabel(u: number): string {
  return u === UnitType.Can ? 'банк.' : u === UnitType.Piece ? 'шт.' : 'л'
}

async function load() {
  loading.value = true
  try {
    chem.value = await chemicalsApi.get(id.value)
  } catch (e) {
    if (e instanceof ApiError && e.status === 404)
      toast.add({ severity: 'error', summary: 'Химия не найдена', life: 3000 })
  } finally {
    loading.value = false
  }
}

function fail(e: unknown, fallback: string) {
  const msg = e instanceof ApiError ? e.message : fallback
  toast.add({ severity: 'error', summary: 'Ошибка', detail: msg, life: 4000 })
}

async function openEdit() {
  if (!chem.value) return
  if (crops.value.length === 0) crops.value = await cropsApi.list()
  edit.value = {
    name: chem.value.name ?? '',
    type: chem.value.type ?? null,
    manufacturer: chem.value.manufacturer ?? '',
    comment: chem.value.comment ?? '',
    cropIds: (chem.value.crops ?? []).map((c) => c.id!),
  }
  editDialog.value = true
}

async function saveEdit() {
  try {
    await chemicalsApi.update(id.value, {
      name: edit.value.name.trim(),
      type: edit.value.type ?? undefined,
      manufacturer: edit.value.manufacturer.trim() || null,
      comment: edit.value.comment.trim() || null,
      cropIds: edit.value.cropIds,
    })
    editDialog.value = false
    toast.add({ severity: 'success', summary: 'Сохранено', life: 2000 })
    await load()
  } catch (e) {
    fail(e, 'Не удалось сохранить')
  }
}

function askArchive() {
  archiveWord.value = ''
  const total = chem.value?.totalLiters ?? 0
  if (total > 0) { archiveDialog.value = true } else { doArchive() }
}

async function doArchive() {
  try {
    await chemicalsApi.archive(id.value, archiveWord.value.trim() || undefined)
    archiveDialog.value = false
    toast.add({ severity: 'success', summary: 'Химия в архиве', life: 2000 })
    await load()
  } catch (e) {
    fail(e, 'Не удалось архивировать')
  }
}

async function doRestore() {
  try {
    await chemicalsApi.restore(id.value)
    toast.add({ severity: 'success', summary: 'Восстановлено', life: 2000 })
    await load()
  } catch (e) {
    fail(e, 'Не удалось восстановить')
  }
}

onMounted(load)
</script>

<template>
  <section class="page" v-if="chem">
    <!-- merged-карточка (ТЗ §18.2) -->
    <PvMessage v-if="isMerged" severity="info" :closable="false">
      Эта карточка объединена с другой.
      <a href="#" @click.prevent="router.push({ name: 'chemical-detail', params: { id: chem.mergedIntoItemId! } })">
        Открыть основную карточку</a>
    </PvMessage>

    <div class="head">
      <div>
        <h1 class="page__title">{{ chem.name }}</h1>
        <div v-if="typeLabel" class="muted">{{ typeLabel }}</div>
        <div v-if="chem.manufacturer" class="muted">{{ chem.manufacturer }}</div>
      </div>
      <PvTag v-if="isArchived" value="В архиве" severity="secondary" />
    </div>

    <div class="crops">
      <PvTag v-for="c in chem.crops" :key="c.id!" :value="c.name" severity="secondary" />
    </div>
    <p v-if="chem.comment" class="comment">{{ chem.comment }}</p>

    <div class="total">Общий остаток: <b>{{ (chem.totalLiters ?? 0).toLocaleString('ru-RU') }} л</b></div>

    <!-- Быстрые действия (ТЗ §15.1) -->
    <div class="actions" v-if="!isMerged">
      <PvButton v-if="!isArchived" label="Приход" icon="pi pi-plus-circle" size="small"
        @click="router.push({ name: 'income', query: { chemicalId: chem.id } })" />
      <PvButton v-if="!isArchived" label="Списать" icon="pi pi-minus-circle" size="small" severity="secondary"
        @click="router.push({ name: 'outcome', query: { chemicalId: chem.id } })" />
      <PvButton v-if="!isArchived" label="Корректировка" icon="pi pi-sliders-h" size="small" severity="secondary"
        @click="router.push({ name: 'corrections', query: { chemicalId: chem.id } })" />
      <PvButton v-if="!isArchived" label="Редактировать" icon="pi pi-pencil" size="small" outlined @click="openEdit" />
      <PvButton v-if="!isArchived" label="В архив" icon="pi pi-inbox" size="small" text severity="danger" @click="askArchive" />
      <PvButton v-if="isArchived" label="Восстановить" icon="pi pi-undo" size="small" @click="doRestore" />
    </div>

    <!-- Остатки по складам (ТЗ §15.2) -->
    <div class="warehouses" v-if="chem.warehouses && chem.warehouses.length">
      <button class="toggle" @click="expanded = !expanded">
        <i class="pi" :class="expanded ? 'pi-chevron-down' : 'pi-chevron-right'" />
        Остатки по складам ({{ chem.warehouses.length }})
      </button>
      <div v-show="expanded" class="wh-list">
        <div v-for="w in chem.warehouses" :key="w.warehouseId!" class="wh">
          <div class="wh__head">Склад {{ w.warehouseNumber }} — <b>{{ (w.totalLiters ?? 0).toLocaleString('ru-RU') }} л</b></div>
          <div class="wh__row">Наливом: {{ (w.looseLiters ?? 0).toLocaleString('ru-RU') }} л</div>
          <div class="wh__row" v-for="g in w.packageGroups" :key="g.id!">
            Полные: {{ g.quantity }} × {{ g.packageVolumeLiters }} л ({{ unitLabel(g.unitType!) }})
          </div>
          <div class="wh__row" v-for="o in w.openedPackages" :key="o.id!">
            Вскрыто: {{ unitLabel(o.unitType!) }} {{ o.initialLiters }} л, осталось {{ o.remainingLiters }} л
          </div>
        </div>
      </div>
    </div>

    <!-- Диалог редактирования -->
    <PvDialog v-model:visible="editDialog" header="Редактировать химию" modal :style="{ width: '30rem' }">
      <div class="field"><span>Название *</span><PvInputText v-model="edit.name" /></div>
      <div class="field"><span>Тип средства</span>
        <PvSelect v-model="edit.type" :options="chemicalTypeOptions" option-label="label"
          option-value="value" placeholder="Не указан" show-clear />
      </div>
      <div class="field"><span>Производитель</span><PvInputText v-model="edit.manufacturer" /></div>
      <div class="field"><span>Культуры *</span>
        <PvMultiSelect v-model="edit.cropIds" :options="cropOptions" option-label="label" option-value="value" filter />
      </div>
      <div class="field"><span>Комментарий</span><PvTextarea v-model="edit.comment" rows="3" auto-resize /></div>
      <template #footer>
        <PvButton label="Отмена" text @click="editDialog = false" />
        <PvButton label="Сохранить" @click="saveEdit" />
      </template>
    </PvDialog>

    <!-- Диалог архивирования с остатком (ТЗ §17.1) -->
    <PvDialog v-model:visible="archiveDialog" header="Архивировать с остатком" modal :style="{ width: '26rem' }">
      <p>У химии есть остаток. Для архивирования введите слово <b>АРХИВ</b>:</p>
      <PvInputText v-model="archiveWord" class="w-full" placeholder="АРХИВ" />
      <template #footer>
        <PvButton label="Отмена" text @click="archiveDialog = false" />
        <PvButton label="В архив" severity="danger" :disabled="archiveWord.trim() !== 'АРХИВ'" @click="doArchive" />
      </template>
    </PvDialog>
  </section>

  <section class="page" v-else-if="loading">
    <PvProgressSpinner />
  </section>
</template>

<style scoped>
.head { display: flex; justify-content: space-between; align-items: flex-start; gap: 1rem; }
.muted { color: #6b7280; }
.crops { display: flex; gap: 0.4rem; flex-wrap: wrap; margin: 0.5rem 0; }
.comment { color: #4b5563; }
.total { margin: 0.75rem 0; font-size: 1.05rem; }
.actions { display: flex; gap: 0.5rem; flex-wrap: wrap; margin-bottom: 1rem; }
.warehouses { border-top: 1px solid #e5e7eb; padding-top: 0.75rem; }
.toggle { background: none; border: none; cursor: pointer; font: inherit; display: flex; align-items: center; gap: 0.4rem; padding: 0; }
.wh-list { display: flex; flex-direction: column; gap: 0.75rem; margin-top: 0.75rem; }
.wh { background: rgba(0,0,0,0.03); border-radius: 8px; padding: 0.6rem 0.8rem; }
.wh__head { margin-bottom: 0.25rem; }
.wh__row { font-size: 0.9rem; color: #4b5563; }
.field { display: flex; flex-direction: column; gap: 0.25rem; margin-bottom: 0.75rem; }
.field > span { font-weight: 600; font-size: 0.9rem; }
.w-full { width: 100%; }
</style>
