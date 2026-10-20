import { ListChecks, Workflow } from "lucide-react";
import { defineModule, NavGroups } from "@/lib/modules/types";
import { RunningCountBadge } from "./components/running-count-badge";
import { Resources, Verbs } from "@/lib/permissions";
import { WorkflowInstancesListPage } from "./pages/list-page";
import { WorkflowInstanceViewerPage } from "./pages/viewer-page";
import { WorkflowInstanceRoutes } from "./routes";

export { WorkflowInstanceRoutes };
export {
  useWorkflowInstances,
  useWorkflowInstance,
  useInstanceJournal,
  useActivityExecutions,
  useExecutionStats,
  useInstanceVariables,
  useCancelWorkflowInstance,
  useDeleteWorkflowInstance,
} from "./hooks";

export const workflowInstancesModule = defineModule({
  id: "workflow-instances",
  name: "Workflow instances",
  core: true,
  routes: [
    {
      path: WorkflowInstanceRoutes.list,
      component: WorkflowInstancesListPage,
      title: "Workflow instances",
      permissions: [{ resource: Resources.workflowInstances, verb: Verbs.view }],
    },
    {
      path: WorkflowInstanceRoutes.view,
      component: WorkflowInstanceViewerPage,
      title: "Workflow instance",
      permissions: [{ resource: Resources.workflowInstances, verb: Verbs.view }],
    },
  ],
  nav: [
    {
      id: "workflows",
      label: "Workflows",
      icon: Workflow,
      group: NavGroups.general.id,
      order: 10,
      children: [
        {
          id: "workflow-instances",
          label: "Instances",
          href: WorkflowInstanceRoutes.list,
          icon: ListChecks,
          order: 10,
          badge: RunningCountBadge,
          permissions: [{ resource: Resources.workflowInstances, verb: Verbs.view }],
        },
      ],
    },
  ],
});
