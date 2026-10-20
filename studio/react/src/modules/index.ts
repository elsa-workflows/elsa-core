import type { StudioModule } from "@/lib/modules/types";
import { alterationsModule } from "./alterations";
import { appearanceModule } from "./appearance";
import { dashboardModule } from "./dashboard";
import { diagnosticsConsoleLogsModule } from "./diagnostics-console-logs";
import { diagnosticsStructuredLogsModule } from "./diagnostics-structured-logs";
import { environmentsModule } from "./environments";
import { labelsModule } from "./labels";
import { secretsModule } from "./secrets";
import { securityModule } from "./security";
import { settingsModule } from "./settings";
import { userTasksModule } from "./user-tasks";
import { workflowDefinitionsModule } from "./workflow-definitions";
import { workflowInstancesModule } from "./workflow-instances";

/**
 * Every module compiled into the studio, in route-resolution order. Core modules are always
 * active; the others are switched on per deployment via `features.<id>.enabled` in the
 * studio configuration (and hidden when the backend lacks the feature they need).
 */
export const studioModules: StudioModule[] = [
  dashboardModule,
  workflowDefinitionsModule,
  workflowInstancesModule,
  environmentsModule,
  labelsModule,
  secretsModule,
  securityModule,
  alterationsModule,
  userTasksModule,
  settingsModule,
  appearanceModule,
  diagnosticsConsoleLogsModule,
  diagnosticsStructuredLogsModule,
];
