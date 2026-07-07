import { createI18n } from 'vue-i18n'
import ru from './locales/ru'

// MVP: только русский. Структура готова к добавлению en (ТЗ §4).
export const i18n = createI18n({
  legacy: false,
  locale: 'ru',
  fallbackLocale: 'ru',
  messages: { ru },
})

export default i18n
