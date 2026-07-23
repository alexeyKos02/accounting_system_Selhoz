// Список таймзон для формы хозяйства.
// Берём актуальный набор IANA-идентификаторов из браузера (Intl.supportedValuesOf).
// Селект оставляем editable — пользователь может ввести и своё значение.

// Фолбэк на случай старых движков без Intl.supportedValuesOf (актуальные для наших регионов).
const FALLBACK_TIMEZONES = [
  'UTC',
  'Europe/Kaliningrad', 'Europe/Moscow', 'Europe/Samara', 'Europe/Volgograd',
  'Asia/Yekaterinburg', 'Asia/Omsk', 'Asia/Novosibirsk', 'Asia/Krasnoyarsk',
  'Asia/Irkutsk', 'Asia/Yakutsk', 'Asia/Vladivostok', 'Asia/Magadan', 'Asia/Kamchatka',
  'Asia/Almaty', 'Asia/Aqtobe', 'Asia/Aqtau', 'Asia/Oral',
  'Asia/Tashkent', 'Asia/Bishkek', 'Asia/Dushanbe', 'Asia/Ashgabat',
  'Europe/Kyiv', 'Europe/Minsk',
]

function loadTimezones(): string[] {
  const supported = (Intl as unknown as { supportedValuesOf?: (key: string) => string[] })
    .supportedValuesOf
  try {
    if (typeof supported === 'function') {
      const zones = supported('timeZone')
      if (Array.isArray(zones) && zones.length > 0) return zones
    }
  } catch {
    // ignore — упадём на фолбэк
  }
  return FALLBACK_TIMEZONES
}

export const timezoneOptions: string[] = loadTimezones()
