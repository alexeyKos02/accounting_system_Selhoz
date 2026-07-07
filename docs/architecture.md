# AgroInventory — архитектура

Учёт складских остатков агрохимии. MVP — только химия, но архитектура рассчитана на
запчасти, технику, авторизацию, роли, аудит, импорт Excel (ТЗ v1).

## Ключевые принципы (ТЗ §31)

1. Источник истины по движениям — `inventory_movements`.
2. Быстрые остатки (`chemical_stock_balances`) хранятся отдельно для производительности.
3. Балансы можно пересчитать из истории операций.
4. Полные упаковки хранятся группами (`package_groups`).
5. Вскрытые упаковки хранятся отдельно (`opened_packages`).
6. Пустые упаковки не хранятся в активных остатках.
7. Все опасные действия пишутся в `audit_logs`.
8. GPT ничего не сохраняет сам — только предложения.
9. Backend готов к будущей авторизации.
10. Frontend mobile/tablet-first.

## Структура репозитория

```
backend/                          → Railway
  AgroInventory.sln
  src/
    AgroInventory.Api             контроллеры, Swagger, middleware, DI-композиция
    AgroInventory.Application     use-cases (сервисы), DTO, валидация, интерфейсы
    AgroInventory.Domain          сущности, enum, доменные инварианты
    AgroInventory.Infrastructure  EF Core/PostgreSQL, S3, GPT, backup-jobs
  tests/
    AgroInventory.Domain.Tests
    AgroInventory.Application.Tests
frontend/
  agro-inventory-web              → GitHub Pages (Vue 3 + TS + PWA + PrimeVue)
docs/
  architecture.md
```

Зависимости слоёв (стрелки внутрь):
`Api → Application → Domain`, `Infrastructure → Application, Domain`.
Ядро (`Application`/`Domain`) не знает про EF Core и HTTP.

## Стек и решения

| | Выбор |
|---|---|
| Backend | C#, ASP.NET Core (.NET 8), EF Core, PostgreSQL, Swagger |
| Бизнес-логика | обычные application-сервисы + DI (без CQRS/MediatR) |
| Валидация | FluentValidation |
| Frontend | Vue 3 + TypeScript + Vite, PWA (`vite-plugin-pwa`) |
| UI kit | PrimeVue (тема Aura) + primeicons |
| Стор / роутер / i18n | Pinia, vue-router, vue-i18n (MVP: только ru) |
| Типы API | `openapi-typescript` из Swagger (`npm run gen:api`) |
| Хостинг | Frontend — GitHub Pages, Backend/DB — Railway, бэкапы — S3-compatible |

## Движок остатков (ядро, этап 4)

Три уровня активных остатков на паре `chemical_id + warehouse_id`:
`loose_liters` → `package_groups` (полные) → `opened_packages` (вскрытые).

- `StockEngine` — чистый доменный сервис (без БД), покрыт юнит-тестами.
- Приход/списание/корректировка пишут `inventory_movements` (+ `inventory_movement_details` для сложных списаний).
- Списание: авто-добор (сначала почти пустые вскрытые → loose → вскрытие новой упаковки) или ручной источник; preview-режим для плана/предупреждений (ТЗ §11).
- Корректировка: 3 режима, подтверждение при изменении >20% (ТЗ §13).
- `RecalculateBalance` восстанавливает баланс из истории.

## Задел на будущее

Создаём таблицы `users/roles/permissions/user_roles/audit_logs`, но в MVP все операции —
от `system user`. `inventory_items.item_type` включает `spare_part`; детали запчастей
(`spare_part_details`, `equipment`, `spare_part_equipment_compatibility`) — будущей миграцией.

## План по этапам

1. **Каркас** — solution, 4 проекта + тесты, Swagger, health-эндпоинты, Vue+PrimeVue+PWA скелет. ✅
2. Домен + БД — сущности, DbContext, первая миграция, seed system user + права, дефолт-настройки.
3. Справочники — crops, warehouses, chemicals CRUD + карточка, архив/восстановление, дубли/merge.
4. Движок остатков — income/outcome/correction + preview + пересчёт, юнит-тесты.
5. История + audit — фильтры, редактирование/soft-delete операций, экран audit log.
6. Инвентаризация склада + дашборд.
7. Инфраструктура — S3 `IBackupStorage`, авто-бэкап job, аварийный экран восстановления, Excel-экспорт.
8. GPT — parse-text / parse-photo / enrich-chemical.
9. PWA-полировка + деплой (GitHub Pages + Railway).

## Локальный запуск

Backend:
```
cd backend
dotnet run --project src/AgroInventory.Api --urls http://localhost:5080
# Swagger: http://localhost:5080/swagger
```

Frontend:
```
cd frontend/agro-inventory-web
npm install
npm run dev            # http://localhost:5173 (проксирует /api на :5080)
npm run gen:api        # регенерация типов из Swagger (бэкенд должен быть запущен)
```
