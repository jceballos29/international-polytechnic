import { NextRequest, NextResponse } from "next/server";

export async function GET(request: NextRequest) {
  // Generar state aleatorio para protección CSRF
  const stateBytes = new Uint8Array(16);
  crypto.getRandomValues(stateBytes);
  const state = btoa(String.fromCharCode(...stateBytes))
    .replace(/\+/g, "-").replace(/\//g, "_").replace(/=/g, "");

  // Generar code_verifier
  const verifierBytes = new Uint8Array(32);
  crypto.getRandomValues(verifierBytes);
  const codeVerifier = btoa(String.fromCharCode(...verifierBytes))
    .replace(/\+/g, "-").replace(/\//g, "_").replace(/=/g, "");

  // Calcular code_challenge = BASE64URL(SHA256(code_verifier))
  const encoder = new TextEncoder();
  const data = encoder.encode(codeVerifier);
  const hash = await crypto.subtle.digest("SHA-256", data);
  const codeChallenge = btoa(String.fromCharCode(...new Uint8Array(hash)))
    .replace(/\+/g, "-").replace(/\//g, "_").replace(/=/g, "");

  // Construir URL del IdP
  const identityUrl =
    process.env.NEXT_PUBLIC_IDENTITY_URL ?? "http://localhost:5000";
  const clientId = process.env.CLIENT_ID ?? "portal";
  const redirectUri =
    process.env.REDIRECT_URI ?? "http://localhost:3002/api/auth/callback";

  const params = new URLSearchParams({
    client_id: clientId,
    redirect_uri: redirectUri,
    response_type: "code",
    code_challenge: codeChallenge,
    code_challenge_method: "S256",
    state,
    scope: "openid profile email",
  });

  const authUrl = `${identityUrl}/oauth/authorize?${params.toString()}`;

  // Guardar state y code_verifier en cookies antes de redirigir
  const response = NextResponse.redirect(authUrl);

  const cookieOptions = {
    httpOnly: true,
    sameSite: "lax" as const,
    secure: process.env.NODE_ENV === "production",
    maxAge: 300, // 5 minutos
    path: "/",
  };

  response.cookies.set("oauth_state", state, cookieOptions);
  response.cookies.set("code_verifier", codeVerifier, cookieOptions);

  // Limpiamos los tokens antiguos localmente para empezar desde cero
  response.cookies.delete("access_token");
  response.cookies.delete("refresh_token");
  response.cookies.delete("token_meta");

  return response;
}