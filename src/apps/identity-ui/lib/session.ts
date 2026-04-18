import { cookies } from "next/headers";

export interface UserSession {
  email: string;
  user_id: string;
}

export interface AppInfo {
  client_id: string;
  name: string;
  description: string | null;
  logo_url: string | null;
  roles: string[];
}

export interface DashboardData {
  user: UserSession;
  apps: AppInfo[];
}

export async function getDashboardData(): Promise<DashboardData | null> {
  const cookieStore = await cookies();
  const sessionId = cookieStore.get("idp_session")?.value;

  if (!sessionId) return null;

  const identityApiUrl =
    process.env.IDENTITY_API_URL ?? "http://localhost:5000";

  try {
    const response = await fetch(`${identityApiUrl}/dashboard/apps`, {
      headers: {
        // Pasar el sessionId al Identity API como cookie
        Cookie: `idp_session=${sessionId}`,
      },
      cache: "no-store",
    });

    if (!response.ok) return null;

    return await response.json();
  } catch {
    return null;
  }
}