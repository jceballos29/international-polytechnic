# International Polytechnic

Sistema de gestión universitaria con autenticación centralizada (SSO).

## Productos

| App | URL local | Puerto |
|---|---|---|
| Identity UI (login) | http://localhost:3000 | 3000 |
| Admin Panel | http://localhost:3001 | 3001 |
| Portal | http://localhost:3002 | 3002 |
| Universitas UI | http://localhost:3003 | 3003 |
| Gradus UI | http://localhost:3004 | 3004 |
| Identity API | http://localhost:5000 | 5000 |
| Universitas API | http://localhost:5001 | 5001 |
| Gradus API | http://localhost:5002 | 5002 |

## Requisitos

- .NET 10 SDK
- Node.js 22+
- pnpm 9+
- Docker Desktop

## Arranque rápido

```bash
# 1. Copiar variables de entorno
cp .env.example .env

# 2. Levantar infraestructura base
docker compose up postgresql redis -d

# 3. Ver el grafo de proyectos
pnpm graph
```

## Documentación

- `docs/plan.md` — Arquitectura completa
- `docs/tasks.md` — Estado de las tareas
- `CLAUDE.md` — Contexto para Claude Code