<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useToast } from 'primevue/usetoast'
import { backupsApi } from '../api/backups'
import type { BackupInfo } from '../api/backups'
import { ApiError } from '../api/http'

// Аварийный экран (ТЗ §24.6): БД недоступна или без схемы. Список бэкапов берём из хранилища
// (оно не зависит от основной БД), даём восстановить или повторить проверку.
const emit = defineEmits<{ (e: 'recheck'): void }>()
const toast = useToast()

const backups = ref<BackupInfo[]>([])
const configured = ref(true)
const loading = ref(false)
const restoringFile = ref<string | null>(null)

async function loadBackups() {
  loading.value = true
  try {
    const res = await backupsApi.list()
    configured.value = res.configured
    backups.value = res.backups ?? []
  } catch {
    backups.value = []
  } finally {
    loading.value = false
  }
}

async function restore(b: BackupInfo) {
  if (!window.confirm(`Восстановить БД из «${b.fileName}»? Текущие данные будут заменены.`)) return
  restoringFile.value = b.fileName!
  try {
    const res = await backupsApi.restore(b.fileName!)
    toast.add({
      severity: 'success', summary: 'БД восстановлена',
      detail: `Строк: ${res.rowsRestored}`, life: 3000,
    })
    emit('recheck')
  } catch (e) {
    toast.add({
      severity: 'error', summary: 'Ошибка',
      detail: e instanceof ApiError ? e.message : 'Не удалось восстановить БД', life: 5000,
    })
  } finally {
    restoringFile.value = null
  }
}

function fmtDate(iso: string) {
  return new Date(iso).toLocaleString('ru-RU')
}

onMounted(loadBackups)
</script>

<template>
  <div class="recovery">
    <div class="card">
      <i class="pi pi-exclamation-triangle icon" />
      <h1>База данных недоступна</h1>
      <p class="lead">
        Не удалось подключиться к базе или в ней нет схемы. Можно восстановить данные из резервной копии
        или повторить попытку подключения.
      </p>

      <div class="toolbar">
        <PvButton label="Проверить снова" icon="pi pi-refresh" @click="emit('recheck')" />
        <PvButton label="Обновить список" icon="pi pi-list" outlined :loading="loading" @click="loadBackups" />
      </div>

      <PvMessage v-if="!configured" severity="warn" :closable="false" class="mt">
        Хранилище бэкапов не настроено — восстановление недоступно.
      </PvMessage>

      <div v-else class="backups">
        <h2>Доступные бэкапы</h2>
        <div v-if="loading" class="muted">Загрузка…</div>
        <div v-else-if="!backups.length" class="muted">Бэкапов не найдено.</div>
        <ul v-else class="list">
          <li v-for="b in backups" :key="b.fileName!">
            <span class="fname">{{ b.fileName }}</span>
            <span class="date">{{ fmtDate(b.createdAt!) }}</span>
            <PvButton label="Восстановить" size="small" severity="danger"
              :loading="restoringFile === b.fileName" @click="restore(b)" />
          </li>
        </ul>
      </div>
    </div>
  </div>
</template>

<style scoped>
.recovery {
  min-height: 100vh; display: flex; align-items: center; justify-content: center;
  padding: 1.5rem; background: #f8fafc;
}
.card {
  max-width: 620px; width: 100%; background: #fff; border-radius: 16px;
  border: 1px solid #e5e7eb; padding: 2rem; box-shadow: 0 10px 40px rgba(0,0,0,0.06);
}
.icon { font-size: 2.5rem; color: #dc2626; }
h1 { margin: 0.75rem 0 0.25rem; font-size: 1.5rem; }
.lead { color: #6b7280; margin: 0 0 1.25rem; }
.toolbar { display: flex; gap: 0.5rem; flex-wrap: wrap; }
.mt { margin-top: 1rem; }
.backups { margin-top: 1.5rem; }
.backups h2 { font-size: 1.05rem; margin: 0 0 0.5rem; }
.muted { color: #6b7280; }
.list { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; gap: 0.5rem; }
.list li {
  display: flex; align-items: center; gap: 0.75rem;
  padding: 0.5rem 0.6rem; border: 1px solid #e5e7eb; border-radius: 8px;
}
.fname { font-family: monospace; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.date { margin-left: auto; color: #6b7280; font-size: 0.85rem; white-space: nowrap; }
</style>
