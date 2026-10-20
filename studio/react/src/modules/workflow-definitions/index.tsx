import { FileCode2, Workflow } from "lucide-react";
import { defineModule, NavGroups } from "@/lib/modules/types";
import { Resources, Verbs } from "@/lib/permissions";
import { WorkflowDefinitionsListPage } from "./pages/list-page";
import { WorkflowDefinitionEditorPage } from "./pages/editor-page";
import { WorkflowDefinitionRoutes } from "./routes";

export { WorkflowDefinitionRoutes };
export { useWorkflowEditor } from "./editor-context";

export const workflowDefinitionsModule = defineModule({
  id: "workflow-definitions",
  name: "Workflow definitions",
  core: true,
  routes: [
    {
      path: WorkflowDefinitionRoutes.list,
      component: WorkflowDefinitionsListPage,
      title: "Workflow definitions",
      permissions: [{ resource: Resources.workflowDefinitions, verb: Verbs.view }],
    },
    {
      path: WorkflowDefinitionRoutes.edit,
      component: WorkflowDefinitionEditorPage,
      title: "Workflow editor",
      permissions: [{ resource: Resources.workflowDefinitions, verb: Verbs.view }],
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
          id: "workflow-definitions",
          label: "Definitions",
          href: WorkflowDefinitionRoutes.list,
          icon: FileCode2,
          order: 0,
          permissions: [{ resource: Resources.workflowDefinitions, verb: Verbs.view }],
        },
      ],
    },
  ],
});
