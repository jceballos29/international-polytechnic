import { NextRequest, NextResponse } from "next/server";

export async function POST(request: NextRequest) {
  const identityUrl =
    process.env.IDENTITY_API_URL ?? "http://localhost:5000";
  const appUrl =
    process.env.NEXT_PUBLIC_APP_URL ?? "http://localhost:3002";

  // Revocar refresh token en el IdP
  const refreshToken = request.cookies.get("refresh_token")?.value;

  if (refreshToken) {
    try {
      await fetch(`${identityUrl}/oauth/revoke`, {
        method: "POST",
        headers: { "Content-Type": "application/x-www-form-urlencoded" },
        body: new URLSearchParams({
          token: refreshToken,
          token_type_hint: "refresh_token",
          client_id: process.env.CLIENT_ID ?? "portal",
          client_secret: process.env.CLIENT_SECRET ?? "",
        }).toString(),
      });
    } catch {
      // Si falla → igual limpiamos cookies locales
    }
  }

  // Limpiar cookies de portal
  const cookieOptions = {
    httpOnly: true,
    sameSite: "lax" as const,
    secure: process.env.NODE_ENV === "production",
    path: "/",
    maxAge: 0,
  };

  // Redirigir al IdP para destruir la sesión SSO
  // post_logout_redirect_uri → después del logout SSO, va al login de identity-ui
  const identityPublicUrl =
    process.env.NEXT_PUBLIC_IDENTITY_URL ?? "http://localhost:5000";
  const identityUiUrl =
    process.env.NEXT_PUBLIC_IDENTITY_UI_URL ?? "http://localhost:3000";

  const logoutUrl = `${identityPublicUrl}/dashboard/logout?post_logout_redirect_uri=${encodeURIComponent(identityUiUrl + "/login")}`;

  const response = NextResponse.json({
    success: true,
    redirect: logoutUrl
  });

  response.cookies.set("access_token", "", cookieOptions);
  response.cookies.set("refresh_token", "", cookieOptions);
  response.cookies.set("token_meta", "", cookieOptions);

  return response;
}