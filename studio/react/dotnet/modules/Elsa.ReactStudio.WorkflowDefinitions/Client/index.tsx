
import { message } from "@elsa-workflows/react-studio-sdk/localization";
import { FileCode2, Workflow } from "lucide-react";
import { defineModule, NavGroups } from "@elsa-workflows/react-studio-sdk";
import { Resources, Verbs } from "@elsa-workflows/react-studio-auth/permissions";
import { WorkflowDefinitionsListPage } from "./pages/list-page";
import { WorkflowDefinitionEditorPage } from "./pages/editor-page";
import { WorkflowDefinitionRoutes } from "./routes";

export { WorkflowDefinitionRoutes };
export { useWorkflowEditor } from "./editor-context";

export const workflowDefinitionsModule = defineModule({
  id: "workflow-definitions",
  name: message("Workflow definitions"),
  core: true,
  routes: [
    {
      path: WorkflowDefinitionRoutes.list,
      component: WorkflowDefinitionsListPage,
      title: message("Workflow definitions"),
      permissions: [{ resource: Resources.workflowDefinitions, verb: Verbs.view }],
    },
    {
      path: WorkflowDefinitionRoutes.edit,
      component: WorkflowDefinitionEditorPage,
      title: message("Workflow editor"),
      permissions: [{ resource: Resources.workflowDefinitions, verb: Verbs.view }],
    },
  ],
  nav: [
    {
      id: "workflows",
      label: message("Workflows"),
      icon: Workflow,
      group: NavGroups.general.id,
      order: 10,
      children: [
        {
          id: "workflow-definitions",
          label: message("Definitions"),
          href: WorkflowDefinitionRoutes.list,
          icon: FileCode2,
          order: 0,
          permissions: [{ resource: Resources.workflowDefinitions, verb: Verbs.view }],
        },
      ],
    },
  ],
});
