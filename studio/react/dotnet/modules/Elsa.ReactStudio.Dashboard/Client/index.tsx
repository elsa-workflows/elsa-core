
import { message } from "@elsa-workflows/react-studio-sdk/localization";
import { LayoutDashboard } from "lucide-react";
import { defineModule, NavGroups } from "@elsa-workflows/react-studio-sdk";
import { DashboardPage } from "./pages/dashboard-page";
import { builtInDashboardWidgets } from "./widgets";

export const dashboardModule = defineModule({
  id: "dashboard",
  name: "Dashboard",
  description: message("Operational overview of the workflow server. Other modules add widgets through `extensions.dashboardWidgets`."),
  core: true,
  routes: [{ path: "/", component: DashboardPage, title: message("Dashboard") }],
  nav: [{ id: "dashboard", label: message("Dashboard"), href: "/", icon: LayoutDashboard, group: NavGroups.general.id, order: 0, match: "exact" }],
  extensions: { dashboardWidgets: builtInDashboardWidgets },
});

export type { DashboardCapability } from "./hooks";
export { useDashboardCapability, useDashboardOverview } from "./hooks";
