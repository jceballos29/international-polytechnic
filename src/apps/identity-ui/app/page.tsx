import { redirect } from "next/navigation";
import { getDashboardData } from "@/lib/session";
import { Dashboard } from "@/components/dashboard";


export default async function Home() {
  const data = await getDashboardData();

  if (!data) {
    redirect("/login");
  }

  return <Dashboard data={data} />;
}