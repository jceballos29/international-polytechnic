"use client";

import { useActionState } from "react";
import { loginAction, type LoginState } from "@/lib/actions";

interface LoginFormProps {
  clientId?: string;
  redirectUri?: string;
  codeChallenge?: string;
  codeChallengeMethod?: string;
  state?: string;
}

const initialState: LoginState = {};

export function LoginForm({
  clientId,
  redirectUri,
  codeChallenge,
  codeChallengeMethod,
  state,
}: LoginFormProps) {
  const [formState, formAction, isPending] = useActionState(
    loginAction,
    initialState
  );

  return (
    <form action={formAction} className="space-y-5">
      {/* Campos OAuth — solo si están presentes */}
      {clientId && <input type="hidden" name="clientId" value={clientId} />}
      {redirectUri && <input type="hidden" name="redirectUri" value={redirectUri} />}
      {codeChallenge && <input type="hidden" name="codeChallenge" value={codeChallenge} />}
      {codeChallengeMethod && <input type="hidden" name="codeChallengeMethod" value={codeChallengeMethod} />}
      {state && <input type="hidden" name="state" value={state} />}

      {formState.error && (
        <div className="rounded-lg bg-red-500/10 border border-red-500/20 px-4 py-3">
          <p className="text-sm text-red-400">{formState.error}</p>
        </div>
      )}

      <div className="space-y-1.5">
        <label htmlFor="email" className="block text-sm font-medium text-slate-300">
          Correo institucional
        </label>
        <input
          id="email" name="email" type="email"
          autoComplete="email" autoFocus required
          disabled={isPending}
          placeholder="usuario@institucion.edu"
          className="w-full rounded-lg bg-white/5 border border-white/10
                     px-4 py-2.5 text-sm text-white placeholder:text-slate-600
                     focus:outline-none focus:ring-2 focus:ring-blue-500/50
                     focus:border-blue-500/50 disabled:opacity-50 transition-colors"
        />
        {formState.fieldErrors?.email && (
          <p className="text-xs text-red-400">{formState.fieldErrors.email[0]}</p>
        )}
      </div>

      <div className="space-y-1.5">
        <label htmlFor="password" className="block text-sm font-medium text-slate-300">
          Contraseña
        </label>
        <input
          id="password" name="password" type="password"
          autoComplete="current-password" required
          disabled={isPending}
          placeholder="••••••••"
          className="w-full rounded-lg bg-white/5 border border-white/10
                     px-4 py-2.5 text-sm text-white placeholder:text-slate-600
                     focus:outline-none focus:ring-2 focus:ring-blue-500/50
                     focus:border-blue-500/50 disabled:opacity-50 transition-colors"
        />
        {formState.fieldErrors?.password && (
          <p className="text-xs text-red-400">{formState.fieldErrors.password[0]}</p>
        )}
      </div>

      <button
        type="submit" disabled={isPending}
        className="w-full rounded-lg bg-blue-600 hover:bg-blue-500
                   active:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed
                   px-4 py-2.5 text-sm font-medium text-white
                   transition-colors duration-150 flex items-center justify-center gap-2"
      >
        {isPending ? (
          <>
            <svg className="animate-spin h-4 w-4" fill="none" viewBox="0 0 24 24">
              <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
              <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
            </svg>
            Verificando...
          </>
        ) : "Iniciar sesión"}
      </button>
    </form>
  );
}