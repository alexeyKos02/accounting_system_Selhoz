// Хелперы для <input type="datetime-local"> ↔ ISO (ТЗ §10.2: по умолчанию сейчас, можно менять).

/** Текущий момент в формате datetime-local (локальное время, без секунд). */
export function nowLocalInput(): string {
  const d = new Date()
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`
}

/** datetime-local → ISO-строка (с таймзоной) для отправки на сервер. */
export function localToIso(local: string): string {
  return new Date(local).toISOString()
}
