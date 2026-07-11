// Русская локаль (MVP). Тексты вынесены из компонентов — задел под i18n (ТЗ §4).
export default {
  app: {
    name: 'AgroInventory',
  },
  nav: {
    dashboard: 'Дашборд',
    chemicals: 'Химия',
    income: 'Приход',
    outcome: 'Списание',
    corrections: 'Корректировка',
    inventoryCheck: 'Инвентаризация склада',
    history: 'История',
    auditLog: 'Audit log',
    crops: 'Культуры',
    warehouses: 'Склады',
    fields: 'Поля',
    archive: 'Архив',
    backups: 'Бэкапы',
    settings: 'Настройки',
    spareParts: 'Запчасти',
  },
  chemical: {
    card: 'Карточка химии',
    create: 'Добавить химию',
  },
  spareParts: {
    comingSoon: 'Раздел будет добавлен позже',
  },
  common: {
    notFound: 'Страница не найдена',
    retry: 'Повторить',
    reload: 'Обновить страницу',
  },
  connection: {
    title: 'Нет соединения с сервером',
    description: 'Операции не сохраняются, пока соединение не восстановится.',
  },
} as const
