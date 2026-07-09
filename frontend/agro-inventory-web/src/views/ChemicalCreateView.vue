<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRouter, onBeforeRouteLeave } from 'vue-router'
import { useToast } from 'primevue/usetoast'
import { chemicalsApi } from '../api/chemicals'
import { cropsApi } from '../api/reference'
import { gptApi } from '../api/gpt'
import type { ChemicalTypeValue, CropDto, DuplicateDto } from '../api/types'
import { chemicalTypeOptions } from '../api/types'
import { ApiError } from '../api/http'

const router = useRouter()
const toast = useToast()

const name = ref('')
const type = ref<ChemicalTypeValue | null>(null)
const manufacturer = ref('')
const comment = ref('')
const selectedCropIds = ref<string[]>([])
const crops = ref<CropDto[]>([])
const saving = ref(false)
const dirty = ref(false)

const duplicates = ref<DuplicateDto[]>([])
let dupTimer: ReturnType<typeof setTimeout> | undefined

const createdId = ref<string | null>(null) // после успешного создания (ТЗ §32)

const newCropName = ref('')
const cropOptions = computed(() => crops.value.map((c) => ({ label: c.name, value: c.id })))

const gptConfigured = ref(false)
const enriching = ref(false)

// Обогащение карточки через ИИ (ТЗ §26): подставляем тип средства, производителя, назначение и известные культуры.
async function enrich() {
  const q = name.value.trim()
  if (!q) {
    toast.add({ severity: 'warn', summary: 'Введите название химии', life: 2500 })
    return
  }
  enriching.value = true
  try {
    const data = await gptApi.enrichChemical(q)
    if (data.type != null && type.value == null) type.value = data.type
    if (data.manufacturer && !manufacturer.value.trim()) manufacturer.value = data.manufacturer
    if (data.comment && !comment.value.trim()) comment.value = data.comment

    const matchedIds = (data.crops ?? []).filter((c) => c.matched && c.id).map((c) => c.id as string)
    if (matchedIds.length) {
      const set = new Set([...selectedCropIds.value, ...matchedIds])
      selectedCropIds.value = [...set]
    }
    const unknown = (data.crops ?? []).filter((c) => !c.matched).map((c) => c.name)

    const notes: string[] = []
    if (unknown.length) notes.push(`Не найдены в справочнике: ${unknown.join(', ')} — добавьте вручную.`)
    if (data.notes) notes.push(data.notes)
    toast.add({
      severity: 'success', summary: 'Данные подобраны',
      detail: notes.length ? notes.join(' ') : 'Проверьте поля перед сохранением.', life: 5000,
    })
  } catch (e) {
    fail(e, 'Не удалось подобрать данные')
  } finally {
    enriching.value = false
  }
}

watch([name, type, manufacturer, comment, selectedCropIds], () => { dirty.value = true }, { deep: true })

watch(name, (v) => {
  clearTimeout(dupTimer)
  const q = v.trim()
  if (q.length < 2) { duplicates.value = []; return }
  dupTimer = setTimeout(async () => {
    try { duplicates.value = await chemicalsApi.duplicates(q) } catch { /* тихо */ }
  }, 350)
})

function fail(e: unknown, fallback: string) {
  const msg = e instanceof ApiError ? e.message : fallback
  toast.add({ severity: 'error', summary: 'Ошибка', detail: msg, life: 4000 })
}

async function quickAddCrop() {
  const n = newCropName.value.trim()
  if (!n) return
  try {
    const crop = await cropsApi.create(n)
    newCropName.value = ''
    crops.value = await cropsApi.list()
    if (crop.id) selectedCropIds.value = [...selectedCropIds.value, crop.id]
  } catch (e) {
    fail(e, 'Не удалось добавить культуру')
  }
}

async function submit() {
  saving.value = true
  try {
    const created = await chemicalsApi.create({
      name: name.value.trim(),
      type: type.value ?? undefined,
      manufacturer: manufacturer.value.trim() || null,
      comment: comment.value.trim() || null,
      cropIds: selectedCropIds.value,
    })
    dirty.value = false
    createdId.value = created.id ?? null
    toast.add({ severity: 'success', summary: 'Химия добавлена', life: 2500 })
  } catch (e) {
    fail(e, 'Не удалось создать карточку')
  } finally {
    saving.value = false
  }
}

// «Добавить ещё» — очищаем всё, остаёмся в форме (ТЗ §32).
function addAnother() {
  createdId.value = null
  name.value = ''
  type.value = null
  manufacturer.value = ''
  comment.value = ''
  selectedCropIds.value = []
  duplicates.value = []
  dirty.value = false
}

function openCard() {
  if (createdId.value) router.push({ name: 'chemical-detail', params: { id: createdId.value } })
}

function useExisting(d: DuplicateDto) {
  if (d.id) router.push({ name: 'chemical-detail', params: { id: d.id } })
}

// Защита от потери данных (ТЗ §26)
onBeforeRouteLeave(() => {
  if (dirty.value && !createdId.value) {
    return window.confirm('У вас есть несохранённые изменения. Выйти без сохранения?')
  }
  return true
})

onMounted(async () => {
  crops.value = await cropsApi.list()
  try { gptConfigured.value = (await gptApi.status()).configured } catch { gptConfigured.value = false }
})
</script>

<template>
  <section class="page form">
    <h1 class="page__title">Добавить химию</h1>

    <!-- Экран результата (ТЗ §32) -->
    <div v-if="createdId" class="result">
      <PvMessage severity="success" :closable="false">Химия добавлена</PvMessage>
      <div class="row">
        <PvButton label="Открыть карточку" icon="pi pi-external-link" @click="openCard" />
        <PvButton label="Добавить ещё" icon="pi pi-plus" outlined @click="addAnother" />
      </div>
    </div>

    <template v-else>
      <label class="field">
        <span>Название *</span>
        <div class="name-row">
          <PvInputText v-model="name" placeholder="Например, Раундап" />
          <PvButton v-if="gptConfigured" label="Заполнить с ИИ" icon="pi pi-sparkles" outlined
            :loading="enriching" @click="enrich" />
        </div>
      </label>

      <PvMessage v-if="duplicates.length" severity="warn" :closable="false" class="dups">
        <div>Похожие карточки уже есть:</div>
        <ul>
          <li v-for="d in duplicates" :key="d.id!">
            <a href="#" @click.prevent="useExisting(d)">{{ d.name }}</a>
            <span v-if="d.manufacturer"> — {{ d.manufacturer }}</span>
          </li>
        </ul>
        <small>Можно использовать существующую или всё равно создать новую.</small>
      </PvMessage>

      <label class="field">
        <span>Тип средства</span>
        <PvSelect v-model="type" :options="chemicalTypeOptions" option-label="label"
          option-value="value" placeholder="Не указан" show-clear />
      </label>

      <label class="field">
        <span>Производитель</span>
        <PvInputText v-model="manufacturer" />
      </label>

      <label class="field">
        <span>Культуры *</span>
        <PvMultiSelect v-model="selectedCropIds" :options="cropOptions" option-label="label"
          option-value="value" placeholder="Выберите культуры" filter />
      </label>

      <div class="quick">
        <PvInputText v-model="newCropName" placeholder="Быстро добавить культуру" @keyup.enter="quickAddCrop" />
        <PvButton icon="pi pi-plus" text @click="quickAddCrop" />
      </div>

      <label class="field">
        <span>Комментарий</span>
        <PvTextarea v-model="comment" rows="3" auto-resize />
      </label>

      <div class="row">
        <PvButton label="Сохранить" icon="pi pi-check" :loading="saving" @click="submit" />
        <PvButton label="Отмена" text @click="router.back()" />
      </div>
    </template>
  </section>
</template>

<style scoped>
.form { max-width: 620px; }
.field { display: flex; flex-direction: column; gap: 0.25rem; margin-bottom: 1rem; }
.field > span { font-weight: 600; font-size: 0.9rem; }
.name-row { display: flex; gap: 0.5rem; align-items: center; }
.name-row :deep(.p-inputtext) { flex: 1; }
.quick { display: flex; gap: 0.5rem; align-items: center; margin: -0.5rem 0 1rem; }
.row { display: flex; gap: 0.5rem; align-items: center; }
.result { display: flex; flex-direction: column; gap: 1rem; }
.dups { margin-bottom: 1rem; }
.dups ul { margin: 0.25rem 0; padding-left: 1.25rem; }
</style>
