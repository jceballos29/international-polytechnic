# Reporte de Auditoría del Workspace — International Polytechnic

**Fecha:** 17 de abril de 2026  
**Versión Nx:** 22.6.5  
**Package Manager:** pnpm  
**Target Framework .NET:** net10.0  
**Framework Frontend:** Next.js 16.1.6 + React 19

---

## Índice

1. [Resumen del Workspace](#1-resumen-del-workspace)
2. [Análisis de Configuración Nx](#2-análisis-de-configuración-nx)
3. [Análisis por Aplicación](#3-análisis-por-aplicación)
4. [Infraestructura](#4-infraestructura)
5. [Sección de Fallas](#5-sección-de-fallas)
6. [Sección de Mejoras](#6-sección-de-mejoras)
7. [Comparativa con Auditoría Anterior](#7-comparativa-con-auditoría-anterior)

---

## 1. Resumen del Workspace

El workspace es un monorepo Nx con 6 aplicaciones organizadas en `apps/`:

| Proyecto       | Tipo     | Tecnología | Puerto | Tags                               |
| -------------- | -------- | ---------- | ------ | ---------------------------------- |
| `Identity.API` | Backend  | .NET 10    | 5000   | `type:backend, scope:identity`     |
| `identity-ui`  | Frontend | Next.js 16 | 3000   | `type:frontend, scope:identity`    |
| `admin-panel`  | Frontend | Next.js 16 | 3001   | `type:frontend, scope:admin-panel` |
| `portal`       | Frontend | Next.js 16 | 3002   | `type:frontend, scope:portal`      |
| `Gradus.API`   | Backend  | .NET 10    | 5001   | `type:backend, scope:gradus`       |
| `gradus-ui`    | Frontend | Next.js 16 | 3003   | `type:frontend, scope:gradus`      |

**Arquitectura .NET:** Ambos backends siguen Clean Architecture con 4 capas:

- `Domain` → `Application` → `Infrastructure` → `API`

**Infraestructura:** PostgreSQL 18 + Redis 8.4 vía Docker Compose.

**Bases de datos:** `identity_db`, `portal_db`, `gradus_db`.

**Directorio `packages/`:** Existe (con `.gitkeep`), preparado para librerías compartidas futuras.

---

## 2. Análisis de Configuración Nx

### 2.1 `nx.json`

| Aspecto                       | Estado            | Detalle                                                                                |
| ----------------------------- | ----------------- | -------------------------------------------------------------------------------------- |
| `$schema`                     | ✅ Correcto       | Apunta al schema local de Nx                                                           |
| `nxCloudId`                   | ✅ Configurado    | Conectado a Nx Cloud                                                                   |
| `analytics`                   | ✅ Habilitado     | `analytics: true`                                                                      |
| `defaultBase`                 | ✅ `main`         | Base correcta para `nx affected`                                                       |
| `namedInputs`                 | ✅ Bien definidos | Excluye correctamente tests, `bin/`, `obj/`, `*.Tests/`                                |
| `targetDefaults.build`        | ✅ Correcto       | `dependsOn: [^build]`, cache habilitado, inputs: production                            |
| `targetDefaults.test`         | ✅ Correcto       | `dependsOn: [^build]`, cache habilitado, outputs: coverage                             |
| `targetDefaults.lint`         | ✅ Correcto       | Cache habilitado                                                                       |
| `targetDefaults.e2e-ci--**/*` | ✅ Correcto       | `dependsOn: [^build]`                                                                  |
| Plugins                       | ✅ Completos      | `@nx/next`, `@nx/js/typescript`, `@nx/eslint`, `@nx/cypress`, `@nx/jest`, `@nx/dotnet` |
| Generators defaults           | ✅                | Next.js: `style: none`, `linter: eslint`                                               |

### 2.2 `project.json` (root)

Define targets compuestos `dev:identity`, `dev:admin`, `dev:portal`, `dev:gradus` y `dev` que usan tags para ejecutar grupos de servicios en paralelo. Diseño correcto y escalable.

### 2.3 `tsconfig.base.json`

| Aspecto                        | Estado | Detalle                                                                            |
| ------------------------------ | ------ | ---------------------------------------------------------------------------------- |
| `strict`                       | ✅     | Habilitado                                                                         |
| `composite` + `declarationMap` | ✅     | Habilitados para project references                                                |
| `module: esnext`               | ✅     | Correcto para bundlers                                                             |
| `moduleResolution: bundler`    | ✅     | Correcto para Next.js                                                              |
| `noUnusedLocals`               | ✅     | Habilitado                                                                         |
| `noImplicitOverride`           | ✅     | Habilitado                                                                         |
| `noImplicitReturns`            | ✅     | Habilitado                                                                         |
| `noFallthroughCasesInSwitch`   | ✅     | Habilitado                                                                         |
| `noEmitOnError`                | ✅     | Habilitado                                                                         |
| `isolatedModules`              | ✅     | Habilitado para compatibilidad con esbuild/swc                                     |
| `customConditions`             | ⚠️     | `["international-polytechnic"]` — condición personalizada (funcional pero inusual) |

### 2.4 `tsconfig.json` (root)

Referencia las 4 apps frontend correctamente vía `references`. **No incluye las apps .NET** (correcto, ya que no son TypeScript).

### 2.5 `pnpm-workspace.yaml`

Define `packages: ["apps/*", "packages/*"]`. Ambos directorios existen. Incluye `allowBuilds` para paquetes nativos (`@parcel/watcher`, `@swc/core`, `cypress`, `less`, `nx`, `sharp`).

### 2.6 `eslint.config.mjs` (root)

Configuración flat config con `@nx/enforce-module-boundaries` habilitado. Las `depConstraints` implementan restricciones reales por tag:

- Backends solo dependen de backends
- Cada scope solo depende de su propio scope + identity (SSO compartido)
- Gradus puede depender también de portal

### 2.7 `.gitignore`

| Sección           | Estado      | Detalle                                          |
| ----------------- | ----------- | ------------------------------------------------ |
| .NET              | ✅ Completo | `bin/`, `obj/`, `*.user`, `.vs/`, `/artifacts/`  |
| Node.js/Next.js   | ✅ Completo | `node_modules/`, `.next/`, `out/`, `dist/`       |
| Nx                | ✅          | `.nx/`                                           |
| C# Dev Kit cache  | ✅          | `**/*.lscache`                                   |
| Variables entorno | ✅          | `.env`, `.env.local`, `**/.env`, `**/.env.local` |
| Secretos          | ✅          | `*.pem`, `*.key`, `*.pfx`, `secrets.yaml`        |
| Docker            | ✅          | `docker-compose.override.yml`                    |
| IDEs              | ✅          | `.vscode/`, `.idea/`                             |
| OS                | ✅          | `.DS_Store`, `Thumbs.db`                         |
| Duplicados        | ✅          | Sin entradas duplicadas                          |

### 2.8 `package.json` (root)

| Aspecto         | Estado | Detalle                                          |
| --------------- | ------ | ------------------------------------------------ |
| Versión Nx      | ✅     | 22.6.5 consistente en todos los paquetes `@nx/*` |
| TypeScript      | ✅     | ~5.9.2                                           |
| Jest            | ✅     | ^30.0.2 con jest-environment-jsdom               |
| ESLint          | ✅     | ^9.8.0 (flat config compatible)                  |
| React           | ✅     | ^19.0.0                                          |
| Next.js         | ✅     | ~16.1.6                                          |
| `private: true` | ✅     | Correcto para monorepo                           |
| Cypress         | ✅     | ^15.8.0                                          |

### 2.9 `jest.config.ts` (root)

Usa `getJestProjectsAsync()` de `@nx/jest` para descubrir proyectos automáticamente. Correcto.

### 2.10 `jest.preset.js` (root)

Extiende `@nx/jest/preset`. Configuración mínima y correcta.

---

## 3. Análisis por Aplicación

### 3.1 Identity.API (.NET)

| Aspecto                 | Estado                                                        |
| ----------------------- | ------------------------------------------------------------- |
| Arquitectura Clean      | ✅ Domain → Application → Infrastructure → API                |
| `Directory.Build.props` | ✅ Centralizado con `TreatWarningsAsErrors`, `Nullable`, etc. |
| Target `dev` en Nx      | ✅ `dotnet watch` con `continuous: true`                      |
| Solution file `.slnx`   | ✅ Incluye src + tests                                        |
| Health endpoint         | ✅ `/health`                                                  |
| CORS                    | ✅ Configurado para puertos 3000-3003                         |
| Swagger/OpenAPI         | ✅ `MapOpenApi()` + `UseSwaggerUI()` en desarrollo            |
| Connection strings      | ✅ En `appsettings.Development.json` para `identity_db`       |
| Proyectos de test       | ✅ xUnit + coverlet (Application, Domain, Integration)        |
| Tags Nx                 | ✅ `type:backend, scope:identity`                             |
| Controllers             | ✅ `AddControllers()` + `MapControllers()`                    |
| HTTPS redirect          | ✅ Solo en producción (`!IsDevelopment`)                      |

**Contenido actual de código:** Solo `Program.cs` — sin entidades, servicios, o controllers definidos aún en las capas Domain/Application/Infrastructure.

### 3.2 Gradus.API (.NET)

| Aspecto                           | Estado                                               |
| --------------------------------- | ---------------------------------------------------- |
| Misma estructura que Identity.API | ✅                                                   |
| `implicitDependencies`            | ✅ `["Identity.API"]` (correcto — solo backend)      |
| Connection string                 | ✅ Apunta a `gradus_db`                              |
| Tags Nx                           | ✅ `type:backend, scope:gradus`                      |
| Swagger endpoint name             | ✅ "Gradus API v1"                                   |
| HTTP client file                  | ✅ Correcto: "Gradus API — HTTP Client", puerto 5001 |

**Contenido actual de código:** Igual que Identity.API — solo `Program.cs` en la capa API.

### 3.3 identity-ui (Next.js)

| Aspecto              | Estado                                                      |
| -------------------- | ----------------------------------------------------------- |
| `project.json`       | ✅ Tags correctos, `implicitDependencies: ["Identity.API"]` |
| Puerto dev           | ✅ 3000                                                     |
| `next.config.js`     | ✅ Usa `@nx/next` compose plugins                           |
| `tsconfig.json`      | ✅ Extiende `tsconfig.base.json`, config Next.js correcta   |
| `tsconfig.spec.json` | ✅ Apunta a `specs/` y `app/` (corregido)                   |
| ESLint               | ✅ Extiende config raíz + `@next/eslint-plugin-next`        |
| Jest                 | ✅ `next/jest` + `@nx/react/plugins/jest`                   |
| `.env.local.example` | ✅ Variables de API documentadas                            |
| Tests                | ⚠️ Solo un test básico de render                            |
| Rutas API ejemplo    | ✅ Eliminadas (directorio `app/api/` vacío)                 |

### 3.4 admin-panel (Next.js)

| Aspecto                          | Estado                                |
| -------------------------------- | ------------------------------------- |
| Misma estructura que identity-ui | ✅                                    |
| Puerto dev                       | ✅ 3001                               |
| `implicitDependencies`           | ✅ `["identity-ui"]`                  |
| Tags                             | ✅ `type:frontend, scope:admin-panel` |
| `.env.local.example`             | ✅ Variables de API documentadas      |

### 3.5 portal (Next.js)

| Aspecto                | Estado                               |
| ---------------------- | ------------------------------------ |
| Puerto dev             | ✅ 3002                              |
| `implicitDependencies` | ✅ `["identity-ui", "Identity.API"]` |
| Tags                   | ✅ `type:frontend, scope:portal`     |
| `.env.local.example`   | ✅ Variables de API documentadas     |

### 3.6 gradus-ui (Next.js)

| Aspecto                | Estado                                                       |
| ---------------------- | ------------------------------------------------------------ |
| Puerto dev             | ✅ 3003                                                      |
| `implicitDependencies` | ⚠️ `["identity-ui", "Identity.API", "Gradus.API", "portal"]` |
| Tags                   | ✅ `type:frontend, scope:gradus`                             |
| `.env.local.example`   | ✅ Variables de API documentadas                             |

**Nota sobre `gradus-ui` → `portal`:** La tabla de dependencias en `docs/nx-runtime.md` lista las dependencias de `gradus-ui` como `["identity-ui", "Identity.API", "Gradus.API"]` sin incluir `portal`. Sin embargo, el `project.json` actual sí incluye `portal`. La dependencia tiene sentido conceptual (usuarios navegan desde portal a gradus), y la regla ESLint `scope:gradus` permite dependencias de `scope:portal`. Funcional, pero la documentación debería actualizarse para reflejar esta decisión.

---

## 4. Infraestructura

### 4.1 Docker Compose

| Aspecto               | Estado                                                        |
| --------------------- | ------------------------------------------------------------- |
| PostgreSQL            | ✅ `postgres:18-alpine` con healthcheck, volumen, init script |
| Redis                 | ✅ `redis:8.4-alpine` con healthcheck, volumen                |
| Red                   | ✅ `ip-network` bridge                                        |
| Contraseña PostgreSQL | ✅ Usa variable `${POSTGRES_DB_PASSWORD}` desde `.env`        |
| `.env.example`        | ✅ Documenta la variable necesaria                            |
| Imágenes fijas        | ✅ Versiones específicas alpine                               |

### 4.2 Init Script (`init-databases.sh`)

Crea 2 bases de datos adicionales: `portal_db` y `gradus_db`. La base `identity_db` se crea automáticamente por `POSTGRES_DB`. Usa `set -e` y `ON_ERROR_STOP=1` (correcto).

### 4.3 Directorios preparativos

- `infrastructure/docker/dockerfiles/` → Solo `.gitkeep` (preparado para Dockerfiles futuros)
- `infrastructure/k8s/` → Solo `.gitkeep` (preparado para manifiestos K8s)
- `packages/` → Solo `.gitkeep` (preparado para librerías compartidas)

---

## 5. Sección de Fallas

### FALLA-01: Volumen de PostgreSQL apunta a `/var/lib/postgresql` en vez de `/var/lib/postgresql/data` [NO APLICA]

**Severidad:** Media  
**Ubicación:** `docker-compose.yml`, línea del volumen de postgresql  
**Estado:** ❌ **NO APLICA** — En PostgreSQL 18, montar en `/var/lib/postgresql` es válido y no interfiere con la inicialización. La imagen maneja internamente el subdirectorio `data`.

---

### FALLA-02: Discrepancia entre `docs/nx-runtime.md` y `gradus-ui/project.json` en dependencias [SOLUCIONADA]

**Severidad:** Baja  
**Ubicación:** `apps/gradus-ui/project.json` vs `docs/nx-runtime.md`  
**Descripción:** La tabla de dependencias explícitas en `docs/nx-runtime.md` listaba `gradus-ui` con dependencias `["identity-ui", "Identity.API", "Gradus.API"]`, pero el `project.json` real incluye también `"portal"`.  
**Corrección aplicada:** Se actualizó la tabla de dependencias en `docs/nx-runtime.md` para incluir `portal` con la razón "navegación desde portal".

---

### FALLA-03: Referencia a `src/` en `exclude` de `tsconfig.json` de las apps frontend [SOLUCIONADA]

**Severidad:** Baja  
**Ubicación:** `apps/identity-ui/tsconfig.json`, `apps/admin-panel/tsconfig.json`, `apps/portal/tsconfig.json`, `apps/gradus-ui/tsconfig.json`  
**Descripción:** El array `exclude` contenía entradas `"src/**/*.spec.ts"` y `"src/**/*.test.ts"` residuales del generador Nx, referenciando un directorio `src/` que no existe.  
**Corrección aplicada:** Se eliminaron las entradas de `src/` del array `exclude` en los 4 `tsconfig.json` de las apps frontend.

---

### FALLA-04: Contraseña de base de datos hardcoded en `appsettings.Development.json` [SOLUCIONADA]

**Severidad:** Media  
**Ubicación:** `apps/identity/src/Identity.API/appsettings.Development.json`, `apps/gradus/src/Gradus.API/appsettings.Development.json`  
**Descripción:** Los connection strings contenían la contraseña `secret` en texto plano.  
**Corrección aplicada:** Se inicializó .NET User Secrets en ambos proyectos API (`dotnet user-secrets init` + `dotnet user-secrets set`) y se eliminó la contraseña de los `appsettings.Development.json`. Los connection strings ahora obtienen el `Password` desde el secret store local del desarrollador.

---

## 6. Sección de Mejoras

### MEJORA-01: Configurar Entity Framework Core en los backends

**Impacto:** Alto  
**Descripción:** Ambos backends tienen connection strings configurados pero no tienen ningún ORM instalado. No hay paquetes NuGet de EF Core en los `.csproj`, ni `DbContext`, ni migraciones. Las capas Infrastructure están vacías.  
**Propuesta:** Instalar EF Core en las capas Infrastructure:

```xml
<!-- Identity.Infrastructure.csproj / Gradus.Infrastructure.csproj -->
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.6" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.6" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.6" />
```

Y crear el `DbContext` correspondiente en cada Infrastructure.

---

### MEJORA-02: Implementar entidades y lógica de dominio

**Impacto:** Alto  
**Descripción:** Las capas Domain y Application de ambos backends están completamente vacías (solo el `.csproj`). No hay entidades, value objects, interfaces de repositorio, ni casos de uso definidos.  
**Propuesta:** Comenzar con las entidades core de cada dominio:

- **Identity**: `User`, `Role`, `RefreshToken`, `AuditLog`
- **Gradus**: Entidades propias del dominio académico (calificaciones, estudiantes, materias, etc.)

---

### MEJORA-03: Reemplazar páginas scaffold por contenido real

**Impacto:** Medio  
**Descripción:** Las 4 apps frontend conservan el contenido scaffold de Nx: una landing page con links a la documentación de Nx, videos de YouTube, y guías de Nx Console. Los `metadata` en `layout.tsx` dicen "Welcome to identity-ui" / "Generated by create-nx-workspace".  
**Propuesta:** Reemplazar con el diseño real de cada aplicación. Como mínimo, actualizar los metadata:

```tsx
// identity-ui/app/layout.tsx
export const metadata = {
	title: 'International Polytechnic — Identity',
	description: 'Sistema de autenticación centralizada',
};
```

---

### MEJORA-04: Configurar autenticación JWT en los backends

**Impacto:** Alto  
**Descripción:** Según la documentación del workspace, `Identity.API` emite tokens JWT y `Gradus.API` los valida. Sin embargo, ninguno de los dos tiene configuración de autenticación/autorización (`AddAuthentication`, `AddAuthorization`, `UseAuthentication`, `UseAuthorization`).  
**Propuesta:** Implementar JWT en Identity.API (emisor) y configurar la validación en Gradus.API (consumidor).

---

### MEJORA-05: Crear librerías compartidas en `packages/`

**Impacto:** Alto  
**Descripción:** El directorio `packages/` existe pero está vacío. Los 4 frontends comparten necesidades de: autenticación, tipos compartidos, componentes UI, y configuración de API client. No hay código reutilizable entre proyectos.  
**Propuesta:**

```
packages/
  ui/          → Componentes compartidos (design system)
  auth/        → Lógica de autenticación (token handling, interceptors)
  api-client/  → Wrapper de fetch tipado con interceptors
  types/       → Interfaces/tipos compartidos entre frontend y backend
```

---

### MEJORA-06: Implementar tests reales

**Impacto:** Alto  
**Descripción:** Todos los proyectos de test .NET tienen solo `UnitTest1.cs` con un test vacío (`[Fact] public void Test1() { }`). Los frontends tienen solo un test básico de render que valida que la página existe.  
**Propuesta:** A medida que se desarrollen features:

- Tests unitarios para Domain entities y Application use cases
- Tests de integración con `WebApplicationFactory` para los APIs
- Tests de componentes React con Testing Library para los frontends
- Considerar añadir configuración de coverage mínimo en CI

---

### MEJORA-07: Configurar middleware y observabilidad

**Impacto:** Medio  
**Descripción:** Los APIs no tienen middleware de logging estructurado, manejo global de excepciones, ni métricas de observabilidad. Solo tienen el pipeline mínimo.  
**Propuesta:**

```csharp
// Logging estructurado
builder.Services.AddSerilog();

// Middleware de excepciones global
app.UseExceptionHandler();

// Health checks enriquecidos
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString)
    .AddRedis(redisConnection);
```

---

### MEJORA-08: Conectar Redis en los backends

**Impacto:** Medio  
**Descripción:** Redis está configurado y corriendo en Docker Compose, pero ningún backend lo referencia. No hay connection string de Redis ni uso para caching o sesiones.  
**Propuesta:** Agregar Redis como cache distribuida y/o store de sesiones:

```json
// appsettings.Development.json
{
	"Redis": {
		"ConnectionString": "localhost:6379"
	}
}
```

```csharp
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration["Redis:ConnectionString"];
});
```

---

### MEJORA-09: Preparar Dockerfiles para las APIs

**Impacto:** Medio  
**Descripción:** El directorio `infrastructure/docker/dockerfiles/` está vacío. Cuando se quiera dockerizar las APIs para staging/producción, se necesitarán Dockerfiles multi-stage.  
**Propuesta:** Crear Dockerfiles multi-stage para ambos backends:

```dockerfile
# infrastructure/docker/dockerfiles/Identity.API.Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src
COPY apps/identity/ .
RUN dotnet publish src/Identity.API/Identity.API.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine
WORKDIR /app
COPY --from=build /app .
EXPOSE 5000
HEALTHCHECK CMD wget -qO- http://localhost:5000/health || exit 1
ENTRYPOINT ["dotnet", "Identity.API.dll"]
```

---

### MEJORA-10: Configurar `next.config.js` con rewrites/proxy para desarrollo

**Impacto:** Medio  
**Descripción:** En desarrollo, los frontends necesitan llamar a los backends. Actualmente CORS está configurado, pero una alternativa más limpia es usar rewrites de Next.js para hacer proxy al backend, evitando problemas de CORS y cookies cross-origin.  
**Propuesta:**

```javascript
const nextConfig = {
	nx: {},
	async rewrites() {
		return [
			{
				source: '/api/identity/:path*',
				destination: 'http://localhost:5000/:path*',
			},
			{
				source: '/api/gradus/:path*',
				destination: 'http://localhost:5001/:path*',
			},
		];
	},
};
```

---

## 7. Comparativa con Auditoría Anterior

### Estado de las Fallas del reporte anterior (`report.md`)

| ID       | Descripción                                | Estado Anterior | Estado Actual                                                              |
| -------- | ------------------------------------------ | --------------- | -------------------------------------------------------------------------- |
| FALLA-01 | `.lscache` no en `.gitignore`              | Reportada       | ✅ **SOLUCIONADA** — Se agregó `**/*.lscache` a `.gitignore`               |
| FALLA-02 | `tsconfig.spec.json` referencia `src/`     | Reportada       | ✅ **SOLUCIONADA** — Ahora apunta a `specs/` y `app/`                      |
| FALLA-03 | `packages/` no existe                      | Reportada       | ✅ **SOLUCIONADA** — Directorio creado con `.gitkeep`                      |
| FALLA-04 | `Gradus.API` depende de `portal`           | Reportada       | ✅ **SOLUCIONADA** — Solo depende de `Identity.API`                        |
| FALLA-05 | `depConstraints` sin restricciones reales  | Reportada       | ✅ **SOLUCIONADA** — Restricciones por tag implementadas                   |
| FALLA-06 | Comentario incorrecto en `Gradus.API.http` | Reportada       | ✅ **SOLUCIONADA** — Dice "Gradus API — HTTP Client"                       |
| FALLA-07 | Tests .NET sin `TreatWarningsAsErrors`     | Reportada       | ✅ **SOLUCIONADA** — `Directory.Build.props` aplica a todos                |
| FALLA-08 | `universitas_db` sin proyecto              | Reportada       | ✅ **SOLUCIONADA** — Renombrada a `portal_db` con proyecto correspondiente |
| FALLA-09 | Entradas duplicadas en `.gitignore`        | Reportada       | ✅ **SOLUCIONADA** — Sin duplicados                                        |

**Resultado: 9/9 fallas solucionadas (100%)**

---

### Estado de las Mejoras del reporte anterior (`report.md`)

| ID        | Descripción                                   | Estado Anterior | Estado Actual                                                                                                                                  |
| --------- | --------------------------------------------- | --------------- | ---------------------------------------------------------------------------------------------------------------------------------------------- |
| MEJORA-01 | `Directory.Build.props` para .NET             | Propuesta       | ✅ **IMPLEMENTADA** — Existe en `apps/identity/` y `apps/gradus/` con `TreatWarningsAsErrors`, `Nullable`, `ImplicitUsings`, `TargetFramework` |
| MEJORA-02 | CORS en backends                              | Propuesta       | ✅ **IMPLEMENTADA** — Ambos `Program.cs` con CORS para puertos 3000-3003                                                                       |
| MEJORA-03 | Connection strings PostgreSQL                 | Propuesta       | ✅ **IMPLEMENTADA** — `appsettings.Development.json` con connection strings                                                                    |
| MEJORA-04 | Swagger UI para desarrollo                    | Propuesta       | ✅ **IMPLEMENTADA** — `MapOpenApi()` + `UseSwaggerUI()` en desarrollo                                                                          |
| MEJORA-05 | Eliminar rutas API de ejemplo                 | Propuesta       | ✅ **IMPLEMENTADA** — `app/api/` vacío en las 4 apps                                                                                           |
| MEJORA-06 | Librerías compartidas                         | Propuesta       | ⏳ **PARCIAL** — Directorio `packages/` creado, pero vacío                                                                                     |
| MEJORA-07 | `enforce-module-boundaries` con reglas reales | Propuesta       | ✅ **IMPLEMENTADA** — Restricciones por tag en `eslint.config.mjs`                                                                             |
| MEJORA-08 | Healthcheck Docker para APIs                  | Propuesta       | ⏳ **DIFERIDA** — Aún sin Dockerfiles                                                                                                          |
| MEJORA-09 | Variables de entorno con validación           | Propuesta       | ✅ **IMPLEMENTADA** — `.env.local.example` en cada frontend + `.env.example` raíz                                                              |
| MEJORA-10 | Versiones fijas de Docker                     | Propuesta       | ✅ **IMPLEMENTADA** — `postgres:18-alpine` y `redis:8.4-alpine`                                                                                |
| MEJORA-11 | Implementar tests reales                      | Propuesta       | ⏳ **DIFERIDA** — Solo tests scaffold                                                                                                          |
| MEJORA-12 | Asegurar contraseña PostgreSQL                | Propuesta       | ✅ **IMPLEMENTADA** — Usa `${POSTGRES_DB_PASSWORD}` con `.env`                                                                                 |
| MEJORA-13 | Volumen PostgreSQL a `/data`                  | Propuesta       | ❌ **NO IMPLEMENTADA** — Sigue apuntando a `/var/lib/postgresql`                                                                               |

**Resultado: 9/13 mejoras implementadas, 3 diferidas, 1 pendiente**

---

### Resumen de Avance

| Categoría                     | Total | Resueltas/Implementadas | Parciales/Diferidas | Pendientes |
| ----------------------------- | ----- | ----------------------- | ------------------- | ---------- |
| Fallas anteriores             | 9     | 9 (100%)                | 0                   | 0          |
| Mejoras anteriores            | 13    | 9 (69%)                 | 3 (23%)             | 1 (8%)     |
| **Nuevas fallas encontradas** | 4     | —                       | —                   | 4          |
| **Nuevas mejoras propuestas** | 10    | —                       | —                   | 10         |

### Conclusión de Avance

El workspace ha tenido un **avance significativo** desde la auditoría anterior:

1. **Todas las 9 fallas se solucionaron** — esto incluye fixes de configuración, higiene del repo, y correcciones de dependencias.
2. **9 de 13 mejoras se implementaron** — CORS, Swagger, connection strings, `Directory.Build.props`, variables de entorno, module boundaries, versiones Docker fijas, y seguridad de credenciales.
3. **1 mejora quedó sin implementar** (MEJORA-13: volumen PostgreSQL), que se reporta como nueva FALLA-01 en esta auditoría.
4. **3 mejoras están diferidas** correctamente — librerías compartidas, tests reales, y Dockerfiles son tasks que dependen de que se desarrolle funcionalidad real primero.

El estado general del workspace es **sólido para su fase actual** (scaffolding + configuración base). Las nuevas fallas encontradas son menores (severidad baja-media). Las nuevas mejoras propuestas apuntan a la **siguiente fase de desarrollo**: implementar lógica de negocio, autenticación, persistencia, y tests.
