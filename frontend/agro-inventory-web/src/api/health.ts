import { API_BASE } from './http'

// Здоровье БД для аварийного экрана восстановления (ТЗ §24.6).
export interface DatabaseHealth {
  canConnect: boolean
  schemaReady: boolean
  error: string | null
}

/**
 * Проверка БД. Эндпоинт отдаёт 503 с телом, когда БД недоступна, — поэтому читаем тело
 * даже при не-2xx. Сетевая ошибка (сервер лёг) трактуется как «БД недоступна».
 */
export async function checkDatabase(): Promise<DatabaseHealth> {
  try {
    const res = await fetch(`${API_BASE}/health/database`)
    const body = (await res.json().catch(() => null)) as Partial<DatabaseHealth> | null
    if (body && typeof body.canConnect === 'boolean') {
      return { canConnect: body.canConnect, schemaReady: body.schemaReady ?? false, error: body.error ?? null }
    }
    return { canConnect: res.ok, schemaReady: res.ok, error: null }
  } catch {
    return { canConnect: false, schemaReady: false, error: 'Нет соединения с сервером' }
  }
}
