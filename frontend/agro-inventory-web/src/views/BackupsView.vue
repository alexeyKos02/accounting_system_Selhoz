<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useToast } from 'primevue/usetoast'
import { backupsApi } from '../api/backups'
import type { BackupInfo } from '../api/backups'
import { ApiError } from '../api/http'

const toast = useToast()
const configured = ref(true)
const backups = ref<BackupInfo[]>([])
const loading = ref(false)
const creating = ref(false)
const restoringFile = ref<string | null>(null)

async function load() {
  loading.value = true
  try {
    const res = await backupsApi.list()
    configured.value = res.configured
    backups.value = res.backups ?? []
  } catch (e) {
    fail(e, 'Не удалось загрузить список бэкапов')
  } finally {
    loading.value = false
  }
}

async function create() {
  creating.value = true
  try {
    await backupsApi.create()
    toast.add({ severity: 'success', summary: 'Бэкап создан', life: 2500 })
    await load()
  } catch (e) {
    fail(e, 'Не удалось создать бэкап')
  } finally {
    creating.value = false
  }
}

async function download(b: BackupInfo) {
  try {
    await backupsApi.download(b.fileName!)
  } catch (e) {
    fail(e, 'Не удалось скачать бэкап')
  }
}

async function restore(b: BackupInfo) {
  const ok = window.confirm(
    `Восстановить БД из «${b.fileName}»?\n\n` +
    'Все текущие справочники, остатки и операции будут заменены данными из бэкапа. ' +
    'Действие необратимо.',
  )
  if (!ok) return
  restoringFile.value = b.fileName!
  try {
    const res = await backupsApi.restore(b.fileName!)
    toast.add({
      severity: 'success', summary: 'БД восстановлена',
      detail: `Таблиц: ${res.tablesRestored}, строк: ${res.rowsRestored}`, life: 4000,
    })
    await load()
  } catch (e) {
    fail(e, 'Не удалось восстановить БД')
  } finally {
    restoringFile.value = null
  }
}

function fail(e: unknown, fb: string) {
  toast.add({ severity: 'error', summary: 'Ошибка', detail: e instanceof ApiError ? e.message : fb, life: 4000 })
}

function fmtSize(bytes?: number) {
  const b = bytes ?? 0
  if (b < 1024) return `${b} Б`
  if (b < 1024 * 1024) return `${(b / 1024).toFixed(1)} КБ`
  return `${(b / 1024 / 1024).toFixed(1)} МБ`
}
function fmtDate(iso: string) {
  return new Date(iso).toLocaleString('ru-RU')
}

onMounted(load)
</script>

<template>
  <section class="page">
    <h1 class="page__title">Бэкапы и восстановление</h1>

    <PvMessage v-if="!configured" severity="warn" :closable="false">
      Хранилище бэкапов не настроено. Задайте S3 или локальную папку в конфигурации сервера (секция <code>Backup</code>).
    </PvMessage>

    <div class="toolbar">
      <PvButton label="Создать бэкап" icon="pi pi-save" :loading="creating" :disabled="!configured" @click="create" />
      <PvButton label="Обновить" icon="pi pi-refresh" outlined :loading="loading" @click="load" />
    </div>

    <PvDataTable :value="backups" :loading="loading" data-key="fileName" class="mt">
      <PvColumn header="Файл" field="fileName" />
      <PvColumn header="Размер"><template #body="{ data }">{{ fmtSize(data.sizeBytes) }}</template></PvColumn>
      <PvColumn header="Создан"><template #body="{ data }">{{ fmtDate(data.createdAt) }}</template></PvColumn>
      <PvColumn header="" style="width: 16rem">
        <template #body="{ data }">
          <div class="actions">
            <PvButton label="Скачать" icon="pi pi-download" text size="small" @click="download(data)" />
            <PvButton label="Восстановить" icon="pi pi-history" text size="small" severity="danger"
              :loading="restoringFile === data.fileName" @click="restore(data)" />
          </div>
        </template>
      </PvColumn>
      <template #empty><div class="empty">Бэкапов пока нет</div></template>
    </PvDataTable>
  </section>
</template>

<style scoped>
.toolbar { display: flex; gap: 0.5rem; margin: 1rem 0 0; flex-wrap: wrap; }
.mt { margin-top: 1rem; }
.actions { display: flex; gap: 0.25rem; }
.empty { padding: 1rem; color: #6b7280; }
</style>
