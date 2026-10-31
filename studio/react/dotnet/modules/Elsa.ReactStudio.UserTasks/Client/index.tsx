
import { message } from "@elsa-workflows/react-studio-sdk/localization";
import { ClipboardCheck, Workflow } from "lucide-react";
import { defineModule, NavGroups } from "@elsa-workflows/react-studio-sdk";
import { Resources, Verbs } from "@elsa-workflows/react-studio-auth/permissions";
import { UserTasksListPage } from "./pages/list-page";
import { UserTaskDetailPage } from "./pages/detail-page";
import { UserTaskRoutes } from "./routes";

const view = { resource: Resources.userTasks, verb: Verbs.view };

/**
 * User tasks: inbox of human tasks raised by workflows (list, detail, claim/release/assign/complete/cancel).
 * Capability-gated at runtime through `GET /user-tasks/capabilities` (see `useUserTaskCapabilities`).
 */
export const userTasksModule = defineModule({
  id: "user-tasks",
  name: message("User tasks"),
  description: message("Human tasks raised by running workflows."),
  requiresServerFeatures: ["Elsa.UserTasks"],
  routes: [
    { path: UserTaskRoutes.list, component: UserTasksListPage, title: message("User tasks"), permissions: [view] },
    { path: UserTaskRoutes.detail, component: UserTaskDetailPage, title: message("User task"), permissions: [view] },
  ],
  nav: [
    {
      // Same id as the core "Workflows" parent so a registry that merges parents by id nests this under it.
      id: "workflows",
      label: message("Workflows"),
      icon: Workflow,
      group: NavGroups.general.id,
      order: 10,
      children: [
        {
          id: "user-tasks",
          label: message("User tasks"),
          href: UserTaskRoutes.list,
          icon: ClipboardCheck,
          order: 20,
          permissions: [view],
        },
      ],
    },
  ],
});
