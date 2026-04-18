"use client";

import { useState } from "react";

export function LogoutButton() {
  const [loading, setLoading] = useState(false);

  async function handleLogout() {
    setLoading(true);
    try {
      const response = await fetch("/api/auth/logout", { method: "POST" });
      const data = await response.json();

      // Redirigir al IdP para destruir la sesión SSO
      // El IdP destruye la sesión y redirige de vuelta a /api/auth/login
      window.location.href = data.redirect;
    } catch {
      window.location.href = "/";
    }
  }

  return (
    <button
      onClick={handleLogout}
      disabled={loading}
      className="px-3 py-1.5 rounded-lg text-xs font-medium
                 bg-white/5 border border-white/8
                 text-slate-400 hover:text-white hover:bg-white/8
                 disabled:opacity-50 transition-colors"
    >
      {loading ? "Cerrando..." : "Cerrar sesión"}
    </button>
  );
}