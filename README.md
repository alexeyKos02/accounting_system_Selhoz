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
