# 🔐 Test Users — Identity API

> Credenciales y configuración seeded automáticamente por `DataSeeder.cs` al arrancar la app en desarrollo.
> **No usar en producción.**

---

## 🌐 Servidor

| Campo    | Valor                    |
|----------|--------------------------|
| Base URL | `http://localhost:5000`  |
| Issuer   | `http://localhost:5000`  |
| OIDC     | `http://localhost:5000/.well-known/openid-configuration` |
| JWKS     | `http://localhost:5000/.well-known/jwks.json` |
| Health   | `http://localhost:5000/health` |

---

## 👤 Usuarios

### Super Admin

| Campo    | Valor                   |
|----------|-------------------------|
| Email    | `admin@localhost.com`   |
| Password | `Admin1234!`            |
| Nombre   | Super Admin             |
| Tenant   | `localhost`             |

**Roles asignados:**

| Aplicación    | Rol            | Descripción              |
|---------------|----------------|--------------------------|
| `admin-panel` | `super_admin`  | Acceso total al sistema  |
| `portal`      | `super_admin`  | Acceso total en portal   |

---

## 🏢 Tenant

| Campo    | Valor                       |
|----------|-----------------------------|
| Nombre   | International Polytechnic   |
| Dominio  | `localhost`                 |

---

## 🖥️ Clientes OAuth (Client Applications)

### `admin-panel`

| Campo            | Valor                                      |
|------------------|--------------------------------------------|
| Client ID        | `admin-panel`                              |
| Client Secret    | `admin-panel-secret-change-me`             |
| Redirect URI     | `http://localhost:3001/api/auth/callback`  |
| Grant Types      | `authorization_code`, `refresh_token`      |
| Scopes           | `openid`, `profile`, `email`               |
| Descripción      | Panel de administración del IdP            |

### `portal`

| Campo            | Valor                                      |
|------------------|--------------------------------------------|
| Client ID        | `portal`                                   |
| Client Secret    | `portal-secret-change-me`                  |
| Redirect URI     | `http://localhost:3002/api/auth/callback`  |
| Grant Types      | `authorization_code`, `refresh_token`      |
| Scopes           | `openid`, `profile`, `email`               |
| Descripción      | App de prueba del flujo OAuth              |

---

## 🔄 Flujo Authorization Code + PKCE (ejemplo con `portal`)

### Paso 1 — Login

```http
POST http://localhost:5000/auth/login
Content-Type: application/json

{
  "email": "admin@localhost.com",
  "password": "Admin1234!",
  "clientId": "portal",
  "redirectUri": "http://localhost:3002/api/auth/callback",
  "codeChallenge": "<code_challenge>",
  "codeChallengeMethod": "S256",
  "state": "random-state-abc123"
}
```

> Respuesta: contiene el `code` de autorización.

### Paso 2 — Intercambiar code por tokens

```http
POST http://localhost:5000/oauth/token
Content-Type: application/x-www-form-urlencoded

grant_type=authorization_code
&code=<code>
&code_verifier=<code_verifier>
&client_id=portal
&client_secret=portal-secret-change-me
&redirect_uri=http://localhost:3002/api/auth/callback
```

### Paso 3 — Renovar access token

```http
POST http://localhost:5000/oauth/token
Content-Type: application/x-www-form-urlencoded

grant_type=refresh_token
&refresh_token=<refresh_token>
&client_id=portal
&client_secret=portal-secret-change-me
```

### Paso 4 — Revocar token (logout)

```http
POST http://localhost:5000/oauth/revoke
Content-Type: application/x-www-form-urlencoded

token=<refresh_token>
&token_type_hint=refresh_token
&client_id=portal
&client_secret=portal-secret-change-me
```

### Paso 5 — UserInfo

```http
GET http://localhost:5000/oauth/userinfo
Authorization: Bearer <access_token>
```

---

## ⚙️ Infraestructura requerida

| Servicio     | Conexión                                    |
|--------------|---------------------------------------------|
| PostgreSQL   | `Host=localhost;Port=5432;Database=identity_db;Username=postgres` |
| Redis        | `localhost:6379`                            |
| identity-ui  | `http://localhost:3000`                     |

---

## 🪙 Configuración JWT

| Campo                    | Valor               |
|--------------------------|---------------------|
| Access Token Expiry      | 15 minutos          |
| Refresh Token Expiry     | 30 días             |
| Private Key              | `keys/private.pem`  |
| Public Key               | `keys/public.pem`   |
| Session Cookie           | `idp_session`       |
| Session TTL              | 24 horas            |

---

> Archivo generado desde `DataSeeder.cs` e `Identity.API.http`.
