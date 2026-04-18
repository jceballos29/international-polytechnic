import { cookies } from "next/headers";
import type { TokenSet, UserInfo } from "@/types/auth";

// ── Constantes ─────────────────────────────────────────────
const IDENTITY_URL = process.env.IDENTITY_API_URL ?? "http://localhost:5000";
const CLIENT_ID = process.env.CLIENT_ID ?? "portal";
const CLIENT_SECRET = process.env.CLIENT_SECRET ?? "";
const REDIRECT_URI =
  process.env.REDIRECT_URI ?? "http://localhost:3002/api/auth/callback";


// ── Token Exchange ─────────────────────────────────────────

/**
 * Intercambia el authorization_code por Access Token + Refresh Token.
 * POST /oauth/token con grant_type=authorization_code.
 */
export async function exchangeCodeForTokens(
  code: string,
  codeVerifier: string,
): Promise<TokenSet> {
  const response = await fetch(`${IDENTITY_URL}/oauth/token`, {
    method: "POST",
    headers: { "Content-Type": "application/x-www-form-urlencoded" },
    body: new URLSearchParams({
      grant_type: "authorization_code",
      code,
      code_verifier: codeVerifier,
      client_id: CLIENT_ID,
      client_secret: CLIENT_SECRET,
      redirect_uri: REDIRECT_URI,
    }).toString(),
  });

  if (!response.ok) {
    const error = await response.json();
    throw new Error(error.error_description ?? "Error al intercambiar el code");
  }

  const data = await response.json();

  return {
    accessToken: data.accessToken,
    refreshToken: data.refreshToken,
    expiresIn: data.expiresIn,
    scope: data.scope,
    issuedAt: Math.floor(Date.now() / 1000),
  };
}

/**
 * Renueva el Access Token usando el Refresh Token.
 */
export async function refreshAccessToken(
  refreshToken: string,
): Promise<TokenSet | null> {
  try {
    const response = await fetch(`${IDENTITY_URL}/oauth/token`, {
      method: "POST",
      headers: { "Content-Type": "application/x-www-form-urlencoded" },
      body: new URLSearchParams({
        grant_type: "refresh_token",
        refresh_token: refreshToken,
        client_id: CLIENT_ID,
        client_secret: CLIENT_SECRET,
      }).toString(),
    });

    if (!response.ok) return null;

    const data = await response.json();

    return {
      accessToken: data.accessToken,
      refreshToken: data.refreshToken,
      expiresIn: data.expiresIn,
      scope: data.scope,
      issuedAt: Math.floor(Date.now() / 1000),
    };
  } catch {
    return null;
  }
}

// ── Sesión (cookies) ───────────────────────────────────────

const COOKIE_OPTIONS = {
  httpOnly: true,
  sameSite: "lax" as const,
  secure: process.env.NODE_ENV === "production",
  path: "/",
};

/**
 * Guarda los tokens en cookies HttpOnly.
 * Las cookies HttpOnly no son accesibles desde JavaScript del browser
 * — protección contra ataques XSS.
 */
export async function saveTokens(tokens: TokenSet): Promise<void> {
  try {
    const cookieStore = await cookies();

    cookieStore.set("access_token", tokens.accessToken, {
      ...COOKIE_OPTIONS,
      maxAge: tokens.expiresIn,
    });

    cookieStore.set("refresh_token", tokens.refreshToken, {
      ...COOKIE_OPTIONS,
      maxAge: 60 * 60 * 24 * 30, // 30 días
    });

    cookieStore.set(
      "token_meta",
      JSON.stringify({
        issuedAt: tokens.issuedAt,
        expiresIn: tokens.expiresIn,
        scope: tokens.scope,
      }),
      {
        ...COOKIE_OPTIONS,
        maxAge: tokens.expiresIn,
      },
    );
  } catch {
    // Attempting to set cookies in a Server Component throws an error.
    // Ignored here just to prevent the crash, but token persistence 
    // will fail until performed by an Action or Route Handler.
  }
}

/**
 * Lee el Access Token de las cookies.
 * Si está expirado, intenta renovarlo con el Refresh Token.
 * Retorna null si no hay sesión válida.
 */
export async function getValidAccessToken(): Promise<string | null> {
  const cookieStore = await cookies();

  const accessToken = cookieStore.get("access_token")?.value;
  const refreshToken = cookieStore.get("refresh_token")?.value;
  const metaRaw = cookieStore.get("token_meta")?.value;

  if (!refreshToken) return null;

  // Si hay access_token y no expiró → retornarlo directamente
  if (accessToken && metaRaw) {
    try {
      const meta = JSON.parse(metaRaw);
      const expiresAt = meta.issuedAt + meta.expiresIn;
      const now = Math.floor(Date.now() / 1000);

      // Margen de 30 segundos para evitar expiración en tránsito
      if (now < expiresAt - 30) return accessToken;
    } catch {
      // meta corrupta → renovar
    }
  }

  // Access token expirado → renovar con refresh token
  const newTokens = await refreshAccessToken(refreshToken);
  if (!newTokens) {
    await clearTokens();
    return null;
  }

  await saveTokens(newTokens);
  return newTokens.accessToken;
}

/**
 * Elimina todos los tokens de las cookies.
 */
export async function clearTokens(): Promise<void> {
  try {
    const cookieStore = await cookies();
    cookieStore.delete("access_token");
    cookieStore.delete("refresh_token");
    cookieStore.delete("token_meta");
  } catch {
    // Attempting to delete cookies in a Server Component throws an error.
    // We catch it so it doesn't crash the render. Wiping out tokens must occur
    // in a Route Handler (e.g. /api/auth/login) or Server Action instead.
  }
}

/**
 * Obtiene la información del usuario autenticado.
 * Llama a GET /oauth/userinfo con el Access Token.
 */
export async function getUserInfo(): Promise<UserInfo | null> {
  const accessToken = await getValidAccessToken();
  if (!accessToken) return null;

  try {
    const response = await fetch(`${IDENTITY_URL}/oauth/userinfo`, {
      headers: { Authorization: `Bearer ${accessToken}` },
      cache: "no-store",
    });

    if (!response.ok) {
      return null;
    }

    const data = await response.json();

    return {
      sub: data.sub,
      email: data.email,
      name: data.name,
      givenName: data.given_name,
      familyName: data.family_name,
      initials: data.initials,
      tenantId: data.tenant_id,
      roles: data.roles ?? [],
      permissions: data.permissions ?? [],
    };
  } catch {
    return null;
  }
}
