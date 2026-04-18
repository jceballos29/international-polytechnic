"use client";

import { useState } from "react";
import type { DashboardData } from "@/lib/session";
import Link from "next/link";

// Mapa de URLs por client_id — en producción vendrían de la DB
const APP_URLS: Record<string, string> = {
  "portal": "http://localhost:3002",
  "admin-panel": "http://localhost:3001",
  "universitas-ui": "http://localhost:3003",
  "gradus-ui": "http://localhost:3004",
};

// Iconos por client_id
function AppIcon({ clientId }: { clientId: string }) {
  const icons: Record<string, string> = {
    "portal": "🌐",
    "admin-panel": "⚙️",
    "universitas-ui": "🎓",
    "gradus-ui": "📊",
  };
  return <span className="text-3xl">{icons[clientId] ?? "📱"}</span>;
}

interface DashboardProps {
  data: DashboardData;
}

export function Dashboard({ data }: DashboardProps) {
  const [loggingOut, setLoggingOut] = useState(false);

  async function handleLogout() {
    setLoggingOut(true);
    await fetch("http://localhost:5000/dashboard/logout", {
      method: "POST",
      credentials: "include",
    });
    window.location.reload();
  }

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
      <div className="absolute top-0 right-0 w-96 h-96 bg-indigo-600/10 rounded-full blur-3xl translate-x-1/2 -translate-y-1/2" />

      <div className="relative max-w-4xl mx-auto px-6 py-12">

        {/* Header */}
        <div className="flex items-center justify-between mb-12">
          <div className="flex items-center gap-4">
            <div className="w-10 h-10 rounded-xl bg-blue-600/20 border border-blue-500/30 flex items-center justify-center">
              <svg className="w-5 h-5 text-blue-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5}
                  d="M9 12.75L11.25 15 15 9.75m-3-7.036A11.959 11.959 0 013.598 6 11.99 11.99 0 003 9.749c0 5.592 3.824 10.29 9 11.623 5.176-1.332 9-6.03 9-11.622 0-1.31-.21-2.571-.598-3.751h-.152c-3.196 0-6.1-1.248-8.25-3.285z" />
              </svg>
            </div>
            <div>
              <h1 className="text-lg font-semibold text-white">
                International Polytechnic
              </h1>
              <p className="text-xs text-slate-500">
                Portal de aplicaciones
              </p>
            </div>
          </div>

          {/* Usuario y logout */}
          <div className="flex items-center gap-4">
            <div className="text-right">
              <p className="text-sm text-white">{data.user.email}</p>
              <p className="text-xs text-slate-500">Sesión activa</p>
            </div>
            <button
              onClick={handleLogout}
              disabled={loggingOut}
              className="px-3 py-1.5 rounded-lg text-xs font-medium
                         bg-white/5 border border-white/8
                         text-slate-400 hover:text-white hover:bg-white/8
                         disabled:opacity-50 transition-colors"
            >
              {loggingOut ? "Cerrando..." : "Cerrar sesión"}
            </button>
          </div>
        </div>

        {/* Bienvenida */}
        <div className="mb-10">
          <h2 className="text-2xl font-semibold text-white mb-1">
            Bienvenido de vuelta
          </h2>
          <p className="text-slate-400">
            Selecciona una aplicación para continuar
          </p>
        </div>

        {/* Grid de apps */}
        {data.apps.length === 0 ? (
          <div className="text-center py-16">
            <p className="text-slate-600 text-sm">
              No tienes aplicaciones asignadas.
            </p>
          </div>
        ) : (
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
            {data.apps.map((app) => (
              <AppCard key={app.client_id} app={app} />
            ))}
          </div>
        )}
      </div>
    </main>
  );
}

function AppCard({ app }: { app: DashboardData["apps"][number] }) {
  const url = APP_URLS[app.client_id];

  return (
    <Link
      href={url ?? "#"}
      target={url ? "_blank" : undefined}
      rel="noopener noreferrer"
      className={`
        group block p-6 rounded-2xl
        bg-white/3 border border-white/8
        hover:bg-white/6 hover:border-white/15
        transition-all duration-200
        ${!url ? "cursor-default opacity-60" : ""}
      `}
    >
      {/* Icono */}
      <div className="mb-4">
        <AppIcon clientId={app.client_id} />
      </div>

      {/* Info */}
      <h3 className="text-base font-semibold text-white mb-1 group-hover:text-blue-300 transition-colors">
        {app.name}
      </h3>

      {app.description && (
        <p className="text-sm text-slate-500 mb-4 line-clamp-2">
          {app.description}
        </p>
      )}

      {/* Roles */}
      <div className="flex flex-wrap gap-1.5">
        {app.roles.map((role) => (
          <span
            key={role}
            className="px-2 py-0.5 rounded-md text-xs
                       bg-blue-500/10 border border-blue-500/20
                       text-blue-400 font-medium"
          >
            {role}
          </span>
        ))}
      </div>
    </Link>
  );
}