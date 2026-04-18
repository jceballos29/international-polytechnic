"use server";

import { cookies } from "next/headers";
import { redirect } from "next/navigation";
import { z } from "zod";

const LoginSchema = z.object({
  email: z.string().email("Email inválido"),
  password: z.string().min(6, "Mínimo 6 caracteres"),
  clientId: z.string().optional(),
  redirectUri: z.string().optional(),
  codeChallenge: z.string().optional(),
  codeChallengeMethod: z.string().optional(),
  state: z.string().optional(),
});

export type LoginState = {
  error?: string;
  fieldErrors?: {
    email?: string[];
    password?: string[];
  };
};

export async function loginAction(
  prevState: LoginState,
  formData: FormData
): Promise<LoginState> {
  const raw = {
    email: formData.get("email"),
    password: formData.get("password"),
    clientId: formData.get("clientId") || undefined,
    redirectUri: formData.get("redirectUri") || undefined,
    codeChallenge: formData.get("codeChallenge") || undefined,
    codeChallengeMethod: formData.get("codeChallengeMethod") || undefined,
    state: formData.get("state") || undefined,
  };

  const parsed = LoginSchema.safeParse(raw);
  if (!parsed.success) {
    return {
      fieldErrors: parsed.error.flatten()
        .fieldErrors as LoginState["fieldErrors"],
    };
  }

  const identityApiUrl =
    process.env.IDENTITY_API_URL ?? "http://localhost:5000";

  try {
    const response = await fetch(`${identityApiUrl}/auth/login`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(parsed.data),
    });

    if (!response.ok) {
      const errorData = await response.json();
      return {
        error:
          errorData.error_description ??
          "Las credenciales son incorrectas.",
      };
    }

    const result = await response.json();

    // Propagar cookie al browser
    const setCookieHeader = response.headers.get("set-cookie");
    if (setCookieHeader) {
      const match = setCookieHeader.match(/idp_session=([^;]+)/);
      if (match) {
        const cookieStore = await cookies();
        cookieStore.set("idp_session", match[1], {
          httpOnly: true,
          sameSite: "lax",
          secure: process.env.NODE_ENV === "production",
          maxAge: 60 * 60 * 24,
          path: "/",
        });
      }
    }

    // Modo directo → dashboard
    if (result.is_direct) {
      redirect("/");
    }

    // Modo OAuth → redirigir a la app con el code
    const callbackUrl = new URL(result.redirect_uri);
    callbackUrl.searchParams.set("code", result.code);
    if (result.state) callbackUrl.searchParams.set("state", result.state);

    redirect(callbackUrl.toString());
  } catch (error) {
    if (error instanceof Error && error.message.includes("NEXT_REDIRECT"))
      throw error;
    return { error: "Error de conexión." };
  }
}