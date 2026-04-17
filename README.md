# International Polytechnic

Sistema de gestión universitaria con autenticación centralizada (SSO).

## Productos

| App                 | URL local             | Puerto |
| ------------------- | --------------------- | ------ |
| Identity UI (login) | http://localhost:3000 | 3000   |
| Admin Panel         | http://localhost:3001 | 3001   |
| Portal              | http://localhost:3002 | 3002   |
| Gradus UI           | http://localhost:3003 | 3003   |
| Identity API        | http://localhost:5000 | 5000   |
| Gradus API          | http://localhost:5001 | 5001   |

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

# 3. Configurar credenciales locales de BD (una sola vez por máquina)
cd src/apps/identity/src/Identity.API
dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Host=localhost;Port=5432;Database=identity_db;Username=postgres;Password=secret"

cd ../../../../apps/gradus/src/Gradus.API
dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Host=localhost;Port=5432;Database=gradus_db;Username=postgres;Password=secret"

# 4. Ver el grafo de proyectos
pnpm graph
```

> **¿Por qué User Secrets?** Las contraseñas se almacenan en
> `~/.microsoft/usersecrets/<id>/secrets.json`, fuera del repositorio.
> Cada desarrollador configura sus propias credenciales locales.
> La contraseña por defecto del entorno Docker es `secret` (ver `.env.example`).

## Comandos de desarrollo

### Levantar todo el workspace

```bash
pnpm nx run workspace:dev
```

### Por módulo (recomendado)

Cada comando levanta únicamente las apps necesarias para ese módulo:

```bash
# Solo Identity (Identity UI + Identity API)
pnpm nx run workspace:dev:identity

# Admin Panel (+ Identity)
pnpm nx run workspace:dev:admin

# Portal (+ Identity)
pnpm nx run workspace:dev:portal

# Gradus (+ Identity + Portal + Gradus API)
pnpm nx run workspace:dev:gradus
```

### Apps individuales

```bash
pnpm nx run identity-ui:dev        # http://localhost:3000
pnpm nx run admin-panel:dev        # http://localhost:3001
pnpm nx run portal:dev             # http://localhost:3002
pnpm nx run gradus-ui:dev          # http://localhost:3003
pnpm nx run Identity.API:dev       # http://localhost:5000
pnpm nx run Gradus.API:dev         # http://localhost:5001
```

## Documentación

- `docs/nx-runtime.md` — Runtime y ejecución de tareas Nx
- `CLAUDE.md` — Contexto para Claude Code
