// Тонкая обёртка над fetch для обращения к API.
// Типизированный клиент из OpenAPI: `npm run gen:api` → src/api/schema.d.ts.

import { authStorage } from './authStorage'

export const API_BASE = import.meta.env.VITE_API_BASE ?? '/api'

export class ApiError extends Error {
  readonly status: number
  constructor(status: number, message: string) {
    super(message)
    this.status = status
  }
}

/** true, если сервер недоступен (нет сети/бэкенд лежит) — для экрана «Нет соединения» (ТЗ §5). */
export class NetworkError extends Error {}

/** Вызывается при окончательной потере авторизации (refresh не удался) — редирект на вход. */
let onUnauthorized: (() => void) | null = null
export function setUnauthorizedHandler(handler: () => void) {
  onUnauthorized = handler
}

export async function apiGet<T>(path: string, init?: RequestInit): Promise<T> {
  return request<T>('GET', path, undefined, init)
}

export async function apiPost<T>(path: string, body?: unknown, init?: RequestInit): Promise<T> {
  return request<T>('POST', path, body, init)
}

export async function apiPut<T>(path: string, body?: unknown, init?: RequestInit): Promise<T> {
  return request<T>('PUT', path, body, init)
}

export async function apiDelete<T>(path: string, init?: RequestInit): Promise<T> {
  return request<T>('DELETE', path, undefined, init)
}

/** Заголовки авторизации и контекста хозяйства (ТЗ §1, §15). */
function authHeaders(): Record<string, string> {
  const headers: Record<string, string> = {}
  const token = authStorage.getAccess()
  if (token) headers['Authorization'] = `Bearer ${token}`
  const companyId = authStorage.getCompanyId()
  if (companyId) headers['X-Company-Id'] = companyId
  return headers
}

// Обновление access-токена по refresh — single-flight, чтобы параллельные 401 не гонялись.
let refreshInFlight: Promise<boolean> | null = null

async function tryRefresh(): Promise<boolean> {
  const refreshToken = authStorage.getRefresh()
  if (!refreshToken) return false
  if (refreshInFlight) return refreshInFlight

  refreshInFlight = (async () => {
    try {
      const res = await fetch(`${API_BASE}/auth/refresh`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ refreshToken }),
      })
      if (!res.ok) return false
      const data = await res.json()
      authStorage.setTokens(data.accessToken, data.refreshToken)
      return true
    } catch {
      return false
    } finally {
      refreshInFlight = null
    }
  })()
  return refreshInFlight
}

/** Скачивание файла (Excel/бэкап): fetch → blob → клик по временной ссылке. */
export async function apiDownload(path: string, fallbackName: string): Promise<void> {
  let response: Response
  try {
    response = await fetch(`${API_BASE}${path}`, { headers: authHeaders() })
  } catch {
    throw new NetworkError('Нет соединения с сервером')
  }
  if (!response.ok) {
    const text = await response.text().catch(() => '')
    throw new ApiError(response.status, text || response.statusText)
  }

  const disposition = response.headers.get('Content-Disposition') ?? ''
  const match = /filename="?([^"]+)"?/.exec(disposition)
  const name = match?.[1] ?? fallbackName

  const blob = await response.blob()
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url
  link.download = name
  document.body.appendChild(link)
  link.click()
  link.remove()
  URL.revokeObjectURL(url)
}

async function request<T>(
  method: string, path: string, body?: unknown, init?: RequestInit, retried = false,
): Promise<T> {
  let response: Response
  try {
    response = await fetch(`${API_BASE}${path}`, {
      method,
      headers: {
        'Content-Type': 'application/json',
        ...authHeaders(),
        ...(init?.headers ?? {}),
      },
      body: body === undefined ? undefined : JSON.stringify(body),
      ...init,
    })
  } catch {
    throw new NetworkError('Нет соединения с сервером')
  }

  // 401: пробуем обновить токен один раз и повторить запрос (ТЗ §1).
  if (response.status === 401 && !retried && !path.startsWith('/auth/')) {
    if (await tryRefresh()) return request<T>(method, path, body, init, true)
    onUnauthorized?.()
    throw new ApiError(401, 'Требуется вход в систему.')
  }

  if (!response.ok) {
    const detail = await extractError(response)
    throw new ApiError(response.status, detail)
  }

  return (response.status === 204 ? undefined : await response.json()) as T
}

// Достаём человекочитаемое сообщение из ProblemDetails/ValidationProblemDetails.
async function extractError(response: Response): Promise<string> {
  const text = await response.text().catch(() => '')
  if (!text) return response.statusText
  try {
    const problem = JSON.parse(text)
    if (problem.errors && typeof problem.errors === 'object') {
      const first = Object.values(problem.errors as Record<string, string[]>)[0]
      if (Array.isArray(first) && first.length) return first[0]
    }
    return problem.detail ?? problem.title ?? text
  } catch {
    return text
  }
}
