import { LayoutDashboard } from "lucide-react";
import { defineModule, NavGroups } from "@/lib/modules/types";
import { DashboardPage } from "./pages/dashboard-page";
import { builtInDashboardWidgets } from "./widgets";

export const dashboardModule = defineModule({
  id: "dashboard",
  name: "Dashboard",
  description: "Operational overview of the workflow server. Other modules add widgets through `extensions.dashboardWidgets`.",
  core: true,
  routes: [{ path: "/", component: DashboardPage, title: "Dashboard" }],
  nav: [{ id: "dashboard", label: "Dashboard", href: "/", icon: LayoutDashboard, group: NavGroups.general.id, order: 0, match: "exact" }],
  extensions: { dashboardWidgets: builtInDashboardWidgets },
});

export type { DashboardCapability } from "./hooks";
export { useDashboardCapability, useDashboardOverview } from "./hooks";
