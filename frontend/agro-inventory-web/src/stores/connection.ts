import { defineStore } from 'pinia'
import { ref } from 'vue'
import { apiGet, NetworkError } from '../api/http'

// Статус связи с сервером (ТЗ §5). Offline-sync в MVP не делаем —
// здесь только определяем «есть соединение / нет» для соответствующего экрана.
export const useConnectionStore = defineStore('connection', () => {
  const online = ref(true)
  const checking = ref(false)

  async function check() {
    checking.value = true
    try {
      await apiGet('/health')
      online.value = true
    } catch (e) {
      if (e instanceof NetworkError) online.value = false
      else online.value = true // сервер ответил (пусть и ошибкой) — соединение есть
    } finally {
      checking.value = false
    }
  }

  return { online, checking, check }
})
