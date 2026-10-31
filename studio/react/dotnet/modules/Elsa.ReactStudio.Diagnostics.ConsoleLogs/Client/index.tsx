
import { message } from "@elsa-workflows/react-studio-sdk/localization";
import { Terminal } from "lucide-react";
import { defineModule, NavGroups } from "@elsa-workflows/react-studio-sdk";
import { Resources, Verbs } from "@elsa-workflows/react-studio-auth/permissions";
import { ConsoleLogStorageWidget } from "./components/storage-widget";
import { ConsoleLogsPage } from "./pages/console-page";

const view = { resource: Resources.consoleLogs, verb: Verbs.view };

export const ConsoleLogRoutes = { console: "/diagnostics/console" } as const;

/** Diagnostics: recent stdout/stderr captured by `Elsa.Diagnostics.ConsoleLogs`, with follow mode. */
export const diagnosticsConsoleLogsModule = defineModule({
  id: "diagnostics-console-logs",
  name: message("Console logs"),
  description: message("Recent console output of the workflow server."),
  requiresServerFeatures: ["Elsa.Diagnostics.ConsoleLogs.ShellFeatures.ConsoleLogs"],
  routes: [{ path: ConsoleLogRoutes.console, component: ConsoleLogsPage, title: message("Console"), permissions: [view] }],
  nav: [{ id: "diagnostics-console", label: message("Console"), href: ConsoleLogRoutes.console, icon: Terminal, group: NavGroups.diagnostics.id, order: 10, permissions: [view] }],
  extensions: {
    dashboardWidgets: [{ id: "console-logs-storage", zone: "status", order: 20, component: ConsoleLogStorageWidget, permissions: [view] }],
  },
});
