import { redirect } from "next/navigation";
import { getUserInfo } from "@/lib/auth";
import { LogoutButton } from "@/components/logout-button";


export default async function Home() {
  const user = await getUserInfo();

  if (!user) redirect("/api/auth/login");

  return (
    <main className="min-h-screen bg-[#0A0F1E] relative overflow-hidden">
      {/* Fondo */}
      <div
        className="absolute inset-0 opacity-[0.03]"
        style={{
          backgroundImage: `
            linear-gradient(rgba(255,255,255,0.1) 1px, transparent 1px),
            linear-gradient(90deg, rgba(255,255,255,0.1) 1px, transparent 1px)
          `,
          backgroundSize: "60px 60px",
        }}
      />
      <div className="absolute top-0 right-0 w-96 h-96 bg-blue-600/10 rounded-full blur-3xl translate-x-1/2 -translate-y-1/2" />

      <div className="relative max-w-3xl mx-auto px-6 py-12">

        {/* Header */}
        <div className="flex items-center justify-between mb-12">
          <div className="flex items-center gap-3">
            <div className="w-9 h-9 rounded-xl bg-blue-600/20 border border-blue-500/30 flex items-center justify-center">
              <svg className="w-4 h-4 text-blue-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5}
                  d="M12 21a9.004 9.004 0 008.716-6.747M12 21a9.004 9.004 0 01-8.716-6.747M12 21c2.485 0 4.5-4.03 4.5-9S14.485 3 12 3m0 18c-2.485 0-4.5-4.03-4.5-9S9.515 3 12 3" />
              </svg>
            </div>
            <span className="text-white font-semibold">Portal</span>
          </div>

          <LogoutButton />
        </div>

        {/* Bienvenida */}
        <div className="mb-10">
          <p className="text-slate-400 text-sm mb-1">Bienvenido de vuelta</p>
          <h1 className="text-2xl font-semibold text-white">
            {user.name ?? user.email}
          </h1>
        </div>

        {/* Info del usuario */}
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 mb-8">

          {/* Email */}
          <InfoCard
            label="Correo"
            value={user.email}
            icon="✉️"
          />

          {/* Tenant */}
          <InfoCard
            label="Tenant ID"
            value={user.tenantId}
            icon="🏢"
            mono
          />

          {/* User ID */}
          <InfoCard
            label="User ID (sub)"
            value={user.sub}
            icon="🔑"
            mono
          />

          {/* Roles */}
          <div className="p-5 rounded-2xl bg-white/3 border border-white/8">
            <div className="flex items-center gap-2 mb-3">
              <span className="text-lg">🎭</span>
              <p className="text-xs text-slate-500 font-medium uppercase tracking-wider">
                Roles en Portal
              </p>
            </div>
            {user.roles.length === 0 ? (
              <p className="text-sm text-slate-600">Sin roles asignados</p>
            ) : (
              <div className="flex flex-wrap gap-2">
                {user.roles.map(role => (
                  <span
                    key={role}
                    className="px-2.5 py-1 rounded-lg text-xs font-medium
                               bg-blue-500/10 border border-blue-500/20 text-blue-400"
                  >
                    {role}
                  </span>
                ))}
              </div>
            )}
          </div>
        </div>

        {/* Token info — muestra que el flujo OAuth funcionó */}
        <div className="p-5 rounded-2xl bg-white/3 border border-white/8">
          <p className="text-xs text-slate-500 font-medium uppercase tracking-wider mb-3">
            Estado de la sesión
          </p>
          <div className="flex items-center gap-2">
            <div className="w-2 h-2 rounded-full bg-emerald-400 animate-pulse" />
            <p className="text-sm text-emerald-400">
              Autenticado via OAuth 2.0 + PKCE
            </p>
          </div>
          <p className="text-xs text-slate-600 mt-2">
            Access Token activo · Refresh Token disponible
          </p>
        </div>

      </div>
    </main>
  );
}

function InfoCard({
  label,
  value,
  icon,
  mono = false,
}: {
  label: string;
  value: string;
  icon: string;
  mono?: boolean;
}) {
  return (
    <div className="p-5 rounded-2xl bg-white/3 border border-white/8">
      <div className="flex items-center gap-2 mb-2">
        <span className="text-lg">{icon}</span>
        <p className="text-xs text-slate-500 font-medium uppercase tracking-wider">
          {label}
        </p>
      </div>
      <p className={`text-sm text-white truncate ${mono ? "font-mono" : ""}`}>
        {value}
      </p>
    </div>
  );
}