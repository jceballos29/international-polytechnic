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

**Infraestructura:** PostgreSQL + Redis vía Docker Compose.

**Bases de datos:** `identity_db`, `universitas_db`, `gradus_db`.

---

## 2. Análisis de Configuración Nx

### 2.1 `nx.json`

| Aspecto                | Estado            | Detalle                                                                                |
| ---------------------- | ----------------- | -------------------------------------------------------------------------------------- |
| `$schema`              | ✅ Correcto       | Apunta al schema local de Nx                                                           |
| `nxCloudId`            | ✅ Configurado    | Conectado a Nx Cloud                                                                   |
| `defaultBase`          | ✅ `main`         | Base correcta para `nx affected`                                                       |
| `namedInputs`          | ✅ Bien definidos | Excluye correctamente tests, `bin/`, `obj/`, `*.Tests/`                                |
| `targetDefaults.build` | ✅ Correcto       | `dependsOn: [^build]`, cache habilitado, inputs: production                            |
| `targetDefaults.test`  | ✅ Correcto       | `dependsOn: [^build]`, cache habilitado, outputs: coverage                             |
| `targetDefaults.lint`  | ✅ Correcto       | Cache habilitado                                                                       |
| Plugins                | ✅ Completos      | `@nx/next`, `@nx/js/typescript`, `@nx/eslint`, `@nx/cypress`, `@nx/jest`, `@nx/dotnet` |
| Generators defaults    | ✅                | Next.js: `style: none`, `linter: eslint`                                               |

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
| `customConditions`             | ⚠️     | `["international-polytechnic"]` — condición personalizada (funcional pero inusual) |

### 2.4 `tsconfig.json` (root)

Referencia las 4 apps frontend correctamente. **No incluye las apps .NET** (correcto, ya que no son TypeScript).

### 2.5 `pnpm-workspace.yaml`

Define `packages: ["apps/*", "packages/*"]`. El directorio `packages/` **no existe** en el workspace.

### 2.6 `eslint.config.mjs` (root)

Configuración flat config con `@nx/enforce-module-boundaries` habilitado. Las `depConstraints` actuales solo tienen la regla wildcard (`sourceTag: "*"` → `onlyDependOnLibsWithTags: ["*"]`), lo que permite cualquier dependencia entre proyectos.

### 2.7 `.gitignore`

Cubre .NET (`bin/`, `obj/`), Node.js (`node_modules/`, `.next/`), Nx (`.nx/`), secretos, Docker, IDEs. Tiene entrada duplicada para `.next` (línea 17 como `**/.next/` y línea 62 como `.next`).

---

## 3. Análisis por Aplicación

### 3.1 Identity.API (.NET)

| Aspecto                 | Estado                                                                  |
| ----------------------- | ----------------------------------------------------------------------- |
| Arquitectura Clean      | ✅ Domain → Application → Infrastructure → API                          |
| `TreatWarningsAsErrors` | ✅ En todos los `.csproj` de src                                        |
| `Nullable: enable`      | ✅ En todos los `.csproj`                                               |
| Target `dev` en Nx      | ✅ `dotnet watch` con `continuous: true`                                |
| Solution file `.slnx`   | ✅ Incluye src + tests                                                  |
| Health endpoint         | ✅ `/health`                                                            |
| Swagger/OpenAPI         | ✅ `AddEndpointsApiExplorer()` + paquete `Microsoft.AspNetCore.OpenApi` |
| Proyectos de test       | ✅ xUnit + coverlet (Application, Domain, Integration)                  |
| Tags Nx                 | ✅ `type:backend, scope:identity`                                       |

### 3.2 Gradus.API (.NET)

| Aspecto                           | Estado                          |
| --------------------------------- | ------------------------------- |
| Misma estructura que Identity.API | ✅                              |
| `implicitDependencies`            | ✅ `["portal", "Identity.API"]` |
| Tags Nx                           | ✅ `type:backend, scope:gradus` |

**Nota:** `Gradus.API/project.json` declara `implicitDependencies: ["portal", "Identity.API"]`. Según la documentación del workspace, Gradus.API valida tokens JWT de Identity.API (correcto), pero la dependencia de `portal` es cuestionable — un backend no debería depender de un frontend.

### 3.3 identity-ui (Next.js)

| Aspecto          | Estado                                                      |
| ---------------- | ----------------------------------------------------------- |
| `project.json`   | ✅ Tags correctos, `implicitDependencies: ["Identity.API"]` |
| Puerto dev       | ✅ 3000                                                     |
| `next.config.js` | ✅ Usa `@nx/next` compose plugins                           |
| `tsconfig.json`  | ✅ Extiende `tsconfig.base.json`, config Next.js correcta   |
| ESLint           | ✅ Extiende config raíz + `@next/eslint-plugin-next`        |
| Jest             | ✅ `next/jest` + `@nx/react/plugins/jest`                   |
| Tests            | ⚠️ Solo un test básico de render                            |

### 3.4 admin-panel (Next.js)

| Aspecto                          | Estado                                |
| -------------------------------- | ------------------------------------- |
| Misma estructura que identity-ui | ✅                                    |
| Puerto dev                       | ✅ 3001                               |
| `implicitDependencies`           | ✅ `["identity-ui"]`                  |
| Tags                             | ✅ `type:frontend, scope:admin-panel` |

### 3.5 portal (Next.js)

| Aspecto                | Estado                               |
| ---------------------- | ------------------------------------ |
| Puerto dev             | ✅ 3002                              |
| `implicitDependencies` | ✅ `["identity-ui", "Identity.API"]` |
| Tags                   | ✅ `type:frontend, scope:portal`     |

### 3.6 gradus-ui (Next.js)

| Aspecto                | Estado                                                       |
| ---------------------- | ------------------------------------------------------------ |
| Puerto dev             | ✅ 3003                                                      |
| `implicitDependencies` | ✅ `["identity-ui", "Identity.API", "Gradus.API", "portal"]` |
| Tags                   | ✅ `type:frontend, scope:gradus`                             |

---

## 4. Infraestructura

### 4.1 Docker Compose

| Aspecto               | Estado                                               |
| --------------------- | ---------------------------------------------------- |
| PostgreSQL            | ✅ Con healthcheck, volumen persistente, init script |
| Redis                 | ✅ Con healthcheck, volumen persistente              |
| Red                   | ✅ `ip-network` bridge                               |
| Contraseña PostgreSQL | ⚠️ Hardcoded (`secret`)                              |

### 4.2 Init Script (`init-databases.sh`)

Crea 3 bases de datos: `identity_db` (por defecto), `universitas_db`, `gradus_db`. Usa `set -e` y `ON_ERROR_STOP=1` (correcto).

**Nota:** Se referencia `universitas_db` pero no existe un proyecto `Universitas` en el workspace.

### 4.3 Directorios vacíos

- `infrastructure/docker/dockerfiles/` → Solo `.gitkeep`
- `infrastructure/k8s/` → Solo `.gitkeep`

---

## 5. Sección de Fallas

### FALLA-01: Archivos `.lscache` no están en `.gitignore` [SOLUCIONADA]

**Severidad:** Media  
**Ubicación:** Todos los proyectos `.csproj` generan archivos `.csproj.lscache`  
**Descripción:** Los archivos `.lscache` son cachés del Language Service de C# Dev Kit y no deben estar en el repositorio. Los propios archivos lo advierten: _"To exclude from version control, add \*.lscache to your .gitignore file"_.  
**Corrección:**

```
# En .gitignore, agregar:
**/*.lscache
```

---

### FALLA-02: `tsconfig.spec.json` referencia directorio `src/` que no existe en apps Next.js [SOLUCIONADA]

**Severidad:** Media  
**Ubicación:** `apps/identity-ui/tsconfig.spec.json`, y probablemente las demás apps  
**Descripción:** El `tsconfig.spec.json` incluye globs como `src/**/*.spec.ts`, `src/**/*.test.ts`, pero los archivos de test están en `specs/`, no en `src/`. Los tests están ubicados en `specs/index.spec.tsx` pero la configuración de TS busca en `src/`.  
**Corrección:**

```jsonc
// Cambiar los includes de src/ a specs/ o app/
"include": [
    "jest.config.ts",
    "jest.config.cts",
    "specs/**/*.spec.ts",
    "specs/**/*.spec.tsx",
    "app/**/*.spec.ts",
    "app/**/*.spec.tsx",
    "specs/**/*.d.ts",
    "app/**/*.d.ts"
]
```

---

### FALLA-03: Directorio `packages/` referenciado en `pnpm-workspace.yaml` no existe [SOLUCIONADA]

**Severidad:** Baja  
**Ubicación:** `pnpm-workspace.yaml`  
**Descripción:** Se declara `packages: ["apps/*", "packages/*"]` pero el directorio `packages/` no existe en el workspace. Esto no causa errores pero es una inconsistencia.  
**Corrección:** Eliminar la entrada `"packages/*"` hasta que se necesite, o crear el directorio si se planean librerías compartidas.

---

### FALLA-04: `Gradus.API` tiene `implicitDependency` de `portal` [SOLUCIONADA]

**Severidad:** Media  
**Ubicación:** `apps/gradus/src/Gradus.API/project.json`  
**Descripción:** Un backend (`Gradus.API`) declara dependencia implícita de un frontend (`portal`). Esto invierte la dirección natural del grafo de dependencias. Según la documentación del workspace en `docs/nx-runtime.md`, la tabla de dependencias explícitas no lista a `portal` como dependencia de `Gradus.API` — solo `Identity.API`.  
**Corrección:**

```jsonc
// apps/gradus/src/Gradus.API/project.json
{
	"implicitDependencies": ["Identity.API"],
	// Eliminar "portal"
}
```

---

### FALLA-05: `depConstraints` en ESLint no aplican restricciones reales [SOLUCIONADA]

**Severidad:** Media  
**Ubicación:** `eslint.config.mjs` (root)  
**Descripción:** La regla `@nx/enforce-module-boundaries` tiene una sola constraint:

```js
{ sourceTag: "*", onlyDependOnLibsWithTags: ["*"] }
```

Esto permite que cualquier proyecto dependa de cualquier otro, lo que hace que la regla de boundaries sea inútil. Dado que el workspace tiene tags bien definidos (`type:frontend`, `type:backend`, `scope:*`), se podrían aprovechar para imponer reglas de dependencia.  
**Corrección:**

```js
depConstraints: [
	// Backends no dependen de frontends
	{
		sourceTag: 'type:backend',
		onlyDependOnLibsWithTags: ['type:backend'],
	},
	// Cada scope solo depende de su propio scope + identity (shared auth)
	{
		sourceTag: 'scope:admin-panel',
		onlyDependOnLibsWithTags: ['scope:admin-panel', 'scope:identity'],
	},
	{
		sourceTag: 'scope:portal',
		onlyDependOnLibsWithTags: ['scope:portal', 'scope:identity'],
	},
	{
		sourceTag: 'scope:gradus',
		onlyDependOnLibsWithTags: [
			'scope:gradus',
			'scope:identity',
			'scope:portal',
		],
	},
	{
		sourceTag: 'scope:identity',
		onlyDependOnLibsWithTags: ['scope:identity'],
	},
];
```

---

### FALLA-06: Comentario incorrecto en `Gradus.API.http` [SOLUCIONADA]

**Severidad:** Baja  
**Ubicación:** `apps/gradus/src/Gradus.API/Gradus.API.http`  
**Descripción:** El comentario dice `# Identity API — HTTP Client` en vez de `# Gradus API — HTTP Client`. Fue copiado de Identity sin actualizar.  
**Corrección:** Cambiar el comentario a `# Gradus API — HTTP Client`.

---

### FALLA-07: Proyectos de test .NET no tienen `TreatWarningsAsErrors` [SOLUCIONADA]

**Severidad:** Baja  
**Ubicación:** Todos los `.csproj` dentro de `tests/` (Identity y Gradus)  
**Descripción:** Los proyectos de producción (`src/`) tienen `TreatWarningsAsErrors: true`, pero los proyectos de test (`tests/`) no lo tienen habilitado. Esto crea inconsistencia en la calidad del código de tests.  
**Corrección:** Agregar `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` a cada `PropertyGroup` de los `.csproj` de test, o mejor aún, crear un `Directory.Build.props` compartido.

---

### FALLA-08: Base de datos `universitas_db` creada sin proyecto correspondiente [SOLUCIONADA]

**Severidad:** Baja  
**Ubicación:** `infrastructure/docker/init-databases.sh`  
**Descripción:** El script crea `universitas_db` y la documenta como "Universitas API", pero no existe ningún proyecto Universitas en el workspace. Es infraestructura huérfana o anticipada.  
**Corrección:** Si el proyecto no está planificado a corto plazo, comentar la creación de esta base de datos para evitar confusión. Si está planificado, documentarlo.

---

### FALLA-09: Entradas duplicadas en `.gitignore` [SOLUCIONADA]

**Severidad:** Baja  
**Ubicación:** `.gitignore`  
**Descripción:** `.next` aparece dos veces: como `**/.next/` (línea ~17) y como `.next` (línea ~62), y `out` también aparece como `**/out/` y `out`.  
**Corrección:** Eliminar las entradas duplicadas al final del archivo (líneas 61-62: `.next` y `out`).

---

## 6. Sección de Mejoras

### MEJORA-01: Crear `Directory.Build.props` para proyectos .NET [IMPLEMENTADA]

**Impacto:** Alto  
**Descripción:** Ambas soluciones .NET (Identity y Gradus) repiten las mismas propiedades en cada `.csproj`:

```xml
<TargetFramework>net10.0</TargetFramework>
<Nullable>enable</Nullable>
<ImplicitUsings>enable</ImplicitUsings>
<TreatWarningsAsErrors>true</TreatWarningsAsErrors>
```

**Propuesta:** Crear un `Directory.Build.props` en la raíz de cada solución (`apps/identity/` y `apps/gradus/`) que centralice estas propiedades. Esto reduce duplicación y facilita actualizaciones futuras (e.g., cambiar target framework).

```xml
<!-- apps/identity/Directory.Build.props -->
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
</Project>
```

---

### MEJORA-02: Configurar CORS en los backends .NET [IMPLEMENTADA]

**Impacto:** Alto  
**Descripción:** Ninguno de los dos backends (.NET) tiene configuración de CORS. Cuando los frontends (puertos 3000-3003) intenten llamar a los APIs (puertos 5000-5001) desde el navegador, las peticiones serán bloqueadas.  
**Propuesta:** Agregar configuración CORS en `Program.cs` de ambos APIs:

```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:3001",
                           "http://localhost:3002", "http://localhost:3003")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
// ...
app.UseCors();
```

---

### MEJORA-03: Configurar connection strings hacia PostgreSQL [IMPLEMENTADA]

**Impacto:** Alto  
**Descripción:** Los `appsettings.json` y `appsettings.Development.json` de ambos APIs no contienen connection strings. Docker Compose ya tiene PostgreSQL corriendo, pero los APIs no saben cómo conectarse.  
**Propuesta:** Agregar connection strings en `appsettings.Development.json`:

```json
{
	"ConnectionStrings": {
		"DefaultConnection": "Host=localhost;Port=5432;Database=identity_db;Username=postgres;Password=secret"
	}
}
```

Y usar `User Secrets` o variables de entorno para producción.

---

### MEJORA-04: Agregar Swagger UI para desarrollo [IMPLEMENTADA]

**Impacto:** Medio  
**Descripción:** Ambos APIs llaman a `AddEndpointsApiExplorer()` y tienen el paquete OpenAPI, pero no configuran Swagger UI. Esto facilitaría el desarrollo y pruebas manuales de endpoints.  
**Propuesta:**

```csharp
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "API v1"));
}
```

---

### MEJORA-05: Eliminar rutas API de ejemplo (`/api/hello`) en frontends [IMPLEMENTADA]

**Impacto:** Bajo  
**Descripción:** Las 4 apps Next.js tienen un route handler de ejemplo en `app/api/hello/route.ts` que retorna `'Hello, from API!'`. Este es código scaffold que debería limpiarse.  
**Propuesta:** Eliminar el directorio `app/api/hello/` de las 4 apps, o reemplazarlo con un health check real si se necesita.

---

### MEJORA-06: Crear librerías compartidas para código común [DIFERIDA]

**Impacto:** Alto  
**Descripción:** Actualmente no hay directorio `packages/` o `libs/` para código compartido. Los frontends comparten la misma dependencia de autenticación (`identity-ui`) pero no hay librerías para:

- Configuración compartida de temas/UI
- Tipos/interfaces compartidos
- Utilidades de autenticación (token handling, interceptors)
- Configuración compartida de API client

**Propuesta:** Crear un directorio `packages/` (ya referenciado en `pnpm-workspace.yaml`) con librerías como:

```
packages/
  ui/          → Componentes compartidos
  auth/        → Lógica de autenticación
  api-client/  → Wrapper de fetch con interceptors
  config/      → Configuración compartida
```

---

### MEJORA-07: Configurar `@nx/enforce-module-boundaries` con reglas reales [IMPLEMENTADA]

**Impacto:** Alto  
**Descripción:** (Relacionado con FALLA-05). Aprovechar los tags existentes para prevenir dependencias incorrectas en compile time. Ver la corrección propuesta en FALLA-05.

---

### MEJORA-08: Agregar Healthcheck para Docker en las APIs .NET [DIFERIDA]

**Impacto:** Medio  
**Descripción:** PostgreSQL y Redis tienen healthchecks en Docker Compose, pero cuando se dockericen los APIs, necesitarán healthchecks que apunten a `/health`. Considerar agregar `depends_on` con condición `service_healthy` cuando se agreguen los servicios de API al compose.  
**Propuesta:** Preparar los Dockerfiles en `infrastructure/docker/dockerfiles/` (actualmente vacío con `.gitkeep`).

---

### MEJORA-09: Agregar soporte de variables de entorno con validación [IMPLEMENTADA]

**Impacto:** Medio  
**Descripción:** Ningún frontend tiene configuración de variables de entorno. Para conectar con los APIs, se necesitarán URLs base.  
**Propuesta:** Crear archivos `.env.local.example` en cada app frontend con las variables esperadas:

```env
NEXT_PUBLIC_IDENTITY_API_URL=http://localhost:5000
NEXT_PUBLIC_GRADUS_API_URL=http://localhost:5001
```

---

### MEJORA-10: Usar `postgres:18` en vez de `postgres:latest` [IMPLEMENTADA]

**Impacto:** Medio  
**Descripción:** `docker-compose.yml` usa `postgres:latest` y `redis:latest`. En un entorno de desarrollo compartido, esto puede causar inconsistencias entre máquinas de diferentes desarrolladores.  
**Propuesta:** Fijar versiones específicas:

```yaml
postgresql:
  image: postgres:18-alpine
redis:
  image: redis:8-alpine
```

---

### MEJORA-11: Implementar tests reales [DIFERIDA]

**Impacto:** Alto  
**Descripción:** Todos los proyectos de test .NET tienen solo `UnitTest1.cs` con un test vacío. Los frontends tienen solo un test de render básico. No hay cobertura de lógica de negocio.  
**Propuesta:** A medida que se desarrollen las features, implementar:

- Tests unitarios para Domain y Application layer
- Tests de integración con `WebApplicationFactory` para los APIs
- Tests de componentes React para los frontends

---

### MEJORA-12: Asegurar la contraseña de PostgreSQL [IMPLEMENTADA]

**Impacto:** Alto (seguridad)  
**Descripción:** La contraseña de PostgreSQL está hardcoded como `secret` en `docker-compose.yml`.  
**Propuesta:** Usar un archivo `.env` para las credenciales:

```yaml
# docker-compose.yml
environment:
  POSTGRES_PASSWORD: ${POSTGRES_DB_PASSWORD}
```

```env
# .env (ya está en .gitignore)
POSTGRES_DB_PASSWORD=secret
```

---

### MEJORA-13: Volumen de PostgreSQL apunta a `/var/lib/postgresql` en vez de `/var/lib/postgresql/data` [IMPLEMENTADA]

**Impacto:** Medio  
**Descripción:** En `docker-compose.yml`, el volumen de PostgreSQL mapea a `/var/lib/postgresql` en lugar de `/var/lib/postgresql/data`. Esto puede causar problemas porque la imagen oficial de PostgreSQL espera que el directorio de datos sea `/var/lib/postgresql/data`. Montar el volumen en el directorio padre puede interferir con el proceso de inicialización.  
**Corrección:**

```yaml
volumes:
  - postgres_data:/var/lib/postgresql/data
```

---

## Resumen Ejecutivo

| Categoría | Total | Críticas | Medias | Bajas |
| --------- | ----- | -------- | ------ | ----- |
| Fallas    | 9     | 0        | 4      | 5     |
| Mejoras   | 13    | —        | —      | —     |

**Estado general:** El workspace tiene una arquitectura bien pensada con buena separación de responsabilidades, tags organizados, y documentación interna (docs/nx-runtime.md). La configuración de Nx es sólida. Las fallas encontradas son principalmente de configuración/higiene y no de arquitectura. Las mejoras propuestas van orientadas a preparar el workspace para la siguiente fase de desarrollo (conectar frontends con backends, autenticación, etc.).
