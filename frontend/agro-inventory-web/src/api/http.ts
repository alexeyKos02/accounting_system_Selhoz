// Тонкая обёртка над fetch для обращения к API.
// Типизированный клиент из OpenAPI генерится позже: `npm run gen:api` → src/api/schema.d.ts.

const API_BASE = import.meta.env.VITE_API_BASE ?? '/api'

export class ApiError extends Error {
  readonly status: number
  constructor(status: number, message: string) {
    super(message)
    this.status = status
  }
}

/** true, если сервер недоступен (нет сети/бэкенд лежит) — для экрана «Нет соединения» (ТЗ §5). */
export class NetworkError extends Error {}

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

async function request<T>(method: string, path: string, body?: unknown, init?: RequestInit): Promise<T> {
  let response: Response
  try {
    response = await fetch(`${API_BASE}${path}`, {
      method,
      headers: { 'Content-Type': 'application/json', ...(init?.headers ?? {}) },
      body: body === undefined ? undefined : JSON.stringify(body),
      ...init,
    })
  } catch {
    throw new NetworkError('Нет соединения с сервером')
  }

  if (!response.ok) {
    const text = await response.text().catch(() => '')
    throw new ApiError(response.status, text || response.statusText)
  }

  return (response.status === 204 ? undefined : await response.json()) as T
}
