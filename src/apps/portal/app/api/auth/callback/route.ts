import { NextRequest, NextResponse } from "next/server";
import { exchangeCodeForTokens, saveTokens } from "@/lib/auth";
import { cookies } from "next/headers";

/// <summary>
/// El IdP redirige aquí después del login exitoso.
/// URL: /api/auth/callback?code=...&state=...
///
/// Pasos:
///   1. Verificar el state (protección CSRF)
///   2. Leer el code_verifier guardado en cookie
///   3. Intercambiar code por tokens
///   4. Guardar tokens en cookies HttpOnly
///   5. Redirigir al dashboard
/// </summary>
export async function GET(request: NextRequest) {
  const { searchParams } = new URL(request.url);
  const code = searchParams.get("code");
  const state = searchParams.get("state");
  const error = searchParams.get("error");

  // ── Error del IdP ──────────────────────────────────────
  if (error) {
    const description = searchParams.get("error_description") ?? error;
    return NextResponse.redirect(
      new URL(
        `/auth-error?message=${encodeURIComponent(description)}`,
        request.url,
      ),
    );
  }

  if (!code) {
    return NextResponse.redirect(
      new URL(
        "/auth-error?message=No+se+recibió+el+código+de+autorización",
        request.url,
      ),
    );
  }

  const cookieStore = await cookies();

  // ── Verificar state (CSRF) ─────────────────────────────
  const savedState = cookieStore.get("oauth_state")?.value;
  if (!savedState || savedState !== state) {
    return NextResponse.redirect(
      new URL(
        "/auth-error?message=Estado+de+autenticación+inválido",
        request.url,
      ),
    );
  }

  // ── Leer code_verifier ─────────────────────────────────
  const codeVerifier = cookieStore.get("code_verifier")?.value;
  if (!codeVerifier) {
    return NextResponse.redirect(
      new URL(
        "/auth-error?message=Sesión+de+autenticación+expirada",
        request.url,
      ),
    );
  }

  try {
    // ── Intercambiar code por tokens ───────────────────
    const tokens = await exchangeCodeForTokens(code, codeVerifier);
    await saveTokens(tokens);

    // ── Limpiar cookies temporales ─────────────────────
    const response = NextResponse.redirect(new URL("/", request.url));

    response.cookies.delete("oauth_state");
    response.cookies.delete("code_verifier");

    return response;
  } catch (err) {
    const message =
      err instanceof Error ? err.message : "Error al procesar la autenticación";

    return NextResponse.redirect(
      new URL(
        `/auth-error?message=${encodeURIComponent(message)}`,
        request.url,
      ),
    );
  }
}
