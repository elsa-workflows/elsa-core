
import { message } from "@elsa-workflows/react-studio-sdk/localization";
import { ScrollText } from "lucide-react";
import { defineModule, NavGroups } from "@elsa-workflows/react-studio-sdk";
import { Resources, Verbs } from "@elsa-workflows/react-studio-auth/permissions";
import { StructuredLogStorageWidget } from "./components/storage-widget";
import { StructuredLogsPage } from "./pages/structured-logs-page";

const view = { resource: Resources.structuredLogs, verb: Verbs.view };

export const StructuredLogRoutes = { list: "/diagnostics/structured-logs" } as const;

/** Diagnostics: structured log events captured by `Elsa.Diagnostics.StructuredLogs`, with follow mode. */
export const diagnosticsStructuredLogsModule = defineModule({
  id: "diagnostics-structured-logs",
  name: message("Structured logs"),
  description: message("Structured log events of the workflow server."),
  requiresServerFeatures: ["Elsa.Diagnostics.StructuredLogs.ShellFeatures.StructuredLogs"],
  routes: [{ path: StructuredLogRoutes.list, component: StructuredLogsPage, title: message("Structured logs"), permissions: [view] }],
  nav: [{ id: "diagnostics-structured-logs", label: message("Structured logs"), href: StructuredLogRoutes.list, icon: ScrollText, group: NavGroups.diagnostics.id, order: 20, permissions: [view] }],
  extensions: {
    dashboardWidgets: [{ id: "structured-logs-storage", zone: "status", order: 10, component: StructuredLogStorageWidget, permissions: [view] }],
  },
});
