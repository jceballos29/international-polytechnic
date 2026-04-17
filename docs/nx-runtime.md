# Nx Runtime — Guía de arquitectura y dependencias

## Conceptos clave

### `implicitDependencies` vs dependencia de runtime

`implicitDependencies` le dice a Nx dos cosas:

1. **`nx affected`**: si cambia el proyecto A, el proyecto B también es "affected"
2. **Build order**: cuando se ejecuta `dependsOn: ["^build"]`, Nx buildea las dependencias antes

Lo que **NO hace**: arrancar un proceso antes que otro. En modo `dev`, todos los procesos
arrancan en paralelo. Las apps deben manejar que la API no esté lista inmediatamente
(reintentos, loading states, etc.).

### Tipos de dependencias

| Tipo       | Cómo se detecta                         | Cuándo usarlo                                                          |
| ---------- | --------------------------------------- | ---------------------------------------------------------------------- |
| `static`   | Nx analiza `import` en el código        | Librerías compartidas en el mismo repo                                 |
| `implicit` | Declarada manualmente en `project.json` | Proyectos en distintos lenguajes (Next.js → .NET) o SSO/redirect flows |

### `namedInputs` — qué invalida el cache

```
default     = todos los archivos del proyecto + nx.json
production  = default, excluyendo archivos de test (*.spec, *.test, *.cy, jest.config, etc.)
              y artefactos .NET (bin/, obj/, *.Tests/)
```

Regla práctica: si modificas un `*.spec.ts`, el cache del `build` de producción
**no se invalida** porque los tests están excluidos del input `production`.

### `targetDefaults` — convenciones globales

Definidos en `nx.json`, se fusionan con la configuración inferida por los plugins.
El proyecto individual siempre tiene mayor prioridad.

```
build  → dependsOn ^build, cache ON, inputs production
test   → dependsOn ^build, cache ON, outputs coverage/
lint   → cache ON
```

### `defaultBase: "main"`

Define desde qué rama compara `nx affected`. Sin esto, el comando no sabe
cuál es la rama base y puede incluir o excluir proyectos incorrectamente en CI.

---

## Grafo de dependencias del workspace

```
Identity.Domain
  └── Identity.Application
        └── Identity.Infrastructure
              └── Identity.API ←─────────────────────────────────┐
                    └── identity-ui                               │
                          ├── admin-panel                         │
                          ├── portal ──────────────────────────── (también directo)
                          └── gradus-ui ─────────────────────────(también directo)
                                │
                                └── Gradus.API (scope:gradus)
                                      └── Gradus.Domain
                                            └── Gradus.Application
                                                  └── Gradus.Infrastructure
```

### Dependencias explícitas por proyecto

| Proyecto       | `implicitDependencies`                                | Razón                                                                           |
| -------------- | ----------------------------------------------------- | ------------------------------------------------------------------------------- |
| `Identity.API` | —                                                     | Raíz de autenticación                                                           |
| `identity-ui`  | `Identity.API`                                        | Login UI, llama directamente al API                                             |
| `admin-panel`  | `identity-ui`                                         | SSO redirect a identity-ui para login                                           |
| `portal`       | `identity-ui`, `Identity.API`                         | SSO redirect + llamadas directas al API                                         |
| `Gradus.API`   | `Identity.API`                                        | Valida tokens JWT emitidos por Identity.API                                     |
| `gradus-ui`    | `identity-ui`, `Identity.API`, `Gradus.API`, `portal` | SSO redirect + validación de token + datos de dominio + navegación desde portal |

**Regla del escenario B (SSO centralizado):** Las apps frontend solo declaran `identity-ui`
como dependencia directa (no `Identity.API`), porque no llaman al API de auth directamente —
solo redirigen al login. Las excepciones son `portal` y `gradus-ui` que sí hacen llamadas
directas a `Identity.API`.

---

## Sistema de tags

Los tags permiten filtrar proyectos en cualquier comando de Nx sin mantener listas manuales.

### Taxonomía usada

```
type:frontend    → apps Next.js
type:backend     → APIs .NET

scope:identity   → Identity.API + identity-ui
scope:admin-panel → admin-panel
scope:portal     → portal
scope:gradus     → Gradus.API + gradus-ui
```

### Uso en comandos

```bash
# Solo builds de frontends
pnpm nx run-many -t build -p tag:type:frontend

# Solo tests del scope gradus
pnpm nx run-many -t test -p tag:scope:gradus

# Build afectado solo en el scope identity
pnpm nx affected -t build -p tag:scope:identity
```

---

## Target `dev` unificado

Para poder usar `pnpm nx run-many -t dev` en todos los proyectos (frontend y backend),
los proyectos .NET exponen un target `dev` que internamente llama a `dotnet watch`.

```json
// apps/identity/src/Identity.API/project.json
{
	"targets": {
		"dev": {
			"executor": "nx:run-commands",
			"continuous": true,
			"options": {
				"cwd": "apps/identity/src/Identity.API",
				"command": "dotnet watch"
			}
		}
	}
}
```

**Por qué `continuous: true`:** Le dice a Nx que este target no termina (es un servidor).
Sin esta flag, Nx podría interpretar que el proceso terminó y marcar el task como fallido.

---

## Escenarios de desarrollo y comandos

### Desde el root `workspace`

```bash
pnpm nx dev:identity workspace    # Identity.API + identity-ui
pnpm nx dev:admin workspace       # identity + admin-panel
pnpm nx dev:portal workspace      # identity + portal
pnpm nx dev:gradus workspace      # identity + portal + Gradus.API + gradus-ui
pnpm nx dev workspace             # todo el workspace
```

### Directamente con `nx run-many` (recomendado para flexibilidad)

```bash
# identity
pnpm nx run-many -t dev -p tag:scope:identity --parallel

# admin-panel
pnpm nx run-many -t dev -p tag:scope:identity,tag:scope:admin-panel --parallel

# portal
pnpm nx run-many -t dev -p tag:scope:identity,tag:scope:portal --parallel

# gradus (incluye portal porque los usuarios navegan desde portal hacia gradus)
pnpm nx run-many -t dev -p tag:scope:identity,tag:scope:portal,tag:scope:gradus --parallel

# todo
pnpm nx run-many -t dev --parallel
```

### CI — solo lo afectado por los cambios

```bash
# Build + test + lint de lo que cambió desde main
pnpm nx affected -t build,test,lint

# Solo build afectado
pnpm nx affected -t build
```

---

## Buenas prácticas

### Al agregar una nueva app frontend

1. Genera con el generador: `pnpm nx g @nx/next:app apps/<nombre>`
2. Crea `apps/<nombre>/project.json` con:
   - `"tags": ["type:frontend", "scope:<nombre>"]`
   - `"implicitDependencies"` apuntando a los backends y/o `identity-ui` que necesite
3. Si forma parte de un fullstack, agrega el scope compartido con su backend

### Al agregar una nueva app .NET

1. Crea la solución en `apps/<nombre>/`
2. Agrega `project.json` en `apps/<nombre>/src/<Nombre>.API/` con:
   - `"tags": ["type:backend", "scope:<nombre>"]`
   - `"implicitDependencies": ["Identity.API"]` (para validación de tokens)
   - El target `dev` apuntando a `dotnet watch`

### Cuándo usar `implicitDependencies` vs código compartido

- **`implicitDependencies`**: cuando la dependencia es entre proyectos en distintos lenguajes
  o cuando es una dependencia de runtime (URLs, redirects, tokens)
- **Librerías compartidas** (`libs/`): cuando dos frontends Next.js comparten componentes,
  hooks o utilidades — Nx detecta la dependencia automáticamente via `import`

### `nx affected` en CI

Con `defaultBase: "main"` configurado, en cada PR solo se compilan, testean y lintean
los proyectos afectados por los cambios. Si cambias `Identity.Domain`, Nx recalcula
toda la cadena y ejecuta lo necesario.
