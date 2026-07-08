<script setup lang="ts">
import { ref, watch } from 'vue'
import { useToast } from 'primevue/usetoast'
import { gptApi } from '../api/gpt'
import type { OperationSuggestionDto } from '../api/gpt'
import { ApiError } from '../api/http'

const props = defineProps<{ visible: boolean }>()
const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void
  (e: 'apply', suggestion: OperationSuggestionDto): void
}>()

const toast = useToast()
const text = ref('')
const fileName = ref('')
const file = ref<File | null>(null)
const loading = ref(false)

// Сбрасываем состояние при каждом открытии.
watch(() => props.visible, (open) => {
  if (open) { text.value = ''; file.value = null; fileName.value = '' }
})

function onFile(event: Event) {
  const input = event.target as HTMLInputElement
  file.value = input.files?.[0] ?? null
  fileName.value = file.value?.name ?? ''
}

function fail(e: unknown, fb: string) {
  toast.add({ severity: 'error', summary: 'Ошибка', detail: e instanceof ApiError ? e.message : fb, life: 4000 })
}

async function recognizeText() {
  if (!text.value.trim()) {
    toast.add({ severity: 'warn', summary: 'Введите текст', life: 2500 })
    return
  }
  loading.value = true
  try {
    const suggestion = await gptApi.parseText(text.value.trim())
    emit('apply', suggestion)
    close()
  } catch (e) {
    fail(e, 'Не удалось распознать текст')
  } finally {
    loading.value = false
  }
}

async function recognizePhoto() {
  if (!file.value) {
    toast.add({ severity: 'warn', summary: 'Выберите фото', life: 2500 })
    return
  }
  loading.value = true
  try {
    const suggestion = await gptApi.parsePhoto(file.value)
    emit('apply', suggestion)
    close()
  } catch (e) {
    fail(e, 'Не удалось распознать фото')
  } finally {
    loading.value = false
  }
}

function close() {
  emit('update:visible', false)
}
</script>

<template>
  <PvDialog :visible="visible" @update:visible="emit('update:visible', $event)"
    header="Распознать операцию (ИИ)" modal :style="{ width: '32rem' }">
    <p class="hint">
      Опишите операцию текстом или загрузите фото накладной/этикетки. Распознанные данные попадут в форму —
      проверьте их перед сохранением.
    </p>

    <div class="block">
      <label class="lbl">Текст</label>
      <PvTextarea v-model="text" rows="3" auto-resize placeholder="Например: приход Раундап 200 л, склад 1" />
      <PvButton label="Распознать текст" icon="pi pi-sparkles" :loading="loading" @click="recognizeText" />
    </div>

    <div class="divider"><span>или</span></div>

    <div class="block">
      <label class="lbl">Фото</label>
      <input type="file" accept="image/*" @change="onFile" />
      <PvButton label="Распознать фото" icon="pi pi-image" outlined :disabled="!file" :loading="loading"
        @click="recognizePhoto" />
    </div>

    <template #footer>
      <PvButton label="Закрыть" text @click="close" />
    </template>
  </PvDialog>
</template>

<style scoped>
.hint { color: #6b7280; margin: 0 0 1rem; }
.block { display: flex; flex-direction: column; gap: 0.5rem; margin-bottom: 0.5rem; }
.lbl { font-weight: 600; font-size: 0.9rem; }
.divider { display: flex; align-items: center; text-align: center; color: #9ca3af; margin: 0.75rem 0; }
.divider::before, .divider::after { content: ''; flex: 1; border-top: 1px solid #e5e7eb; }
.divider span { padding: 0 0.75rem; font-size: 0.85rem; }
</style>
