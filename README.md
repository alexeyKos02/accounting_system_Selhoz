# AgroInventory

Веб-приложение для учёта складских остатков агрохимии (MVP).

- **Backend:** `backend/` — ASP.NET Core (.NET 8) + EF Core + PostgreSQL → Railway
- **Frontend:** `frontend/agro-inventory-web` — Vue 3 + TS + PWA → GitHub Pages
- **Архитектура и план:** [docs/architecture.md](docs/architecture.md)

## Быстрый старт

```bash
# Backend
cd backend
dotnet run --project src/AgroInventory.Api --urls http://localhost:5080
# → Swagger: http://localhost:5080/swagger

# Frontend (в другом терминале)
cd frontend/agro-inventory-web
npm install
npm run dev          # → http://localhost:5173
```

## Деплой

### Backend → Railway

1. Создайте в Railway проект и добавьте **PostgreSQL**.
2. Добавьте сервис из этого репозитория, **Root Directory = `backend`** (сборка по [`backend/Dockerfile`](backend/Dockerfile), конфиг — [`backend/railway.toml`](backend/railway.toml)).
3. Переменные окружения сервиса:
   - `ConnectionStrings__Default` — строка подключения к Postgres (можно сослаться на переменные плагина БД Railway).
   - `Cors__AllowedOrigins__0` = `https://alexeykos02.github.io` (адрес фронта на Pages).
   - Опционально бэкапы: `Backup__S3__BucketName`, `Backup__S3__ServiceUrl`, `Backup__S3__AccessKey`, `Backup__S3__SecretKey`.
   - Опционально GPT: `Gpt__ApiKey` (OpenAI).
4. Миграции применяются автоматически при старте (`Database:MigrateOnStartup`, по умолчанию `true`).
5. Порт берётся из `$PORT` (Railway задаёт сам). Health-check: `/api/health`.

### Frontend → GitHub Pages

Деплой автоматический через [`.github/workflows/deploy-frontend.yml`](.github/workflows/deploy-frontend.yml) при пуше в `main` (изменения в `frontend/**`).

1. В настройках репозитория: **Settings → Pages → Source = GitHub Actions**.
2. Добавьте **repository variable** `VITE_API_BASE` = `https://<app>.up.railway.app/api` (адрес бэкенда на Railway).
3. `VITE_BASE` уже задан в workflow (`/accounting_system_Selhoz/`).

Итог: фронт — `https://alexeykos02.github.io/accounting_system_Selhoz/`.

