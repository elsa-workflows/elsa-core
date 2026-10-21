import { Wrench } from "lucide-react";
import { defineModule, NavGroups } from "@/lib/modules/types";
import { Resources, Verbs } from "@/lib/permissions";
import { AlterationDialogHost } from "./components/alteration-dialog-host";
import { useAlterationDialogs, type AlterTarget } from "./dialog-store";
import { AlterationPlansPage } from "./pages/plans-page";
import { AlterationPlanPage } from "./pages/plan-page";
import { AlterationRoutes } from "./routes";

const execute = { resource: Resources.alterations, verb: Verbs.execute };
const view = { resource: Resources.alterations, verb: Verbs.view };

/**
 * Alterations: compose and apply changes (migrate, modify variable, schedule/cancel activity, cancel)
 * to running workflow instances, follow submitted plans, and bulk-retry faulted instances.
 */
export const alterationsModule = defineModule({
  id: "alterations",
  name: "Alterations",
  description: "Alter running workflow instances and follow alteration plans.",
  requiresServerFeatures: ["Elsa.Alterations"],
  Provider: AlterationDialogHost,
  routes: [
    { path: AlterationRoutes.plans, component: AlterationPlansPage, title: "Alteration plans", permissions: [view] },
    { path: AlterationRoutes.plan, component: AlterationPlanPage, title: "Alteration plan", permissions: [view] },
  ],
  nav: [
    {
      id: "alterations",
      label: "Alterations",
      href: AlterationRoutes.plans,
      icon: Wrench,
      group: NavGroups.general.id,
      order: 30,
      permissions: [view],
    },
  ],
  extensions: {
    instanceRowActions: [
      {
        id: "alterations:alter",
        label: "Alter…",
        icon: Wrench,
        permissions: [execute],
        isVisible: (instance) => instance.status === "Running",
        onSelect: (instance, helpers) => {
          const target: AlterTarget = {
            workflowInstanceId: instance.id,
            definitionId: instance.definitionId,
            version: instance.version,
            name: instance.name,
          };
          useAlterationDialogs.getState().openAlter([target], helpers.refresh);
        },
      },
    ],
    instanceViewerActions: [
      {
        id: "alterations:alter",
        label: "Alter",
        icon: Wrench,
        permissions: [execute],
        isVisible: (instance) => instance.status === "Running",
        onSelect: (instance, helpers) => {
          const target: AlterTarget = {
            workflowInstanceId: instance.id,
            definitionId: instance.definitionId,
            version: instance.version,
            name: instance.name,
            faultedActivityIds: Array.from(new Set(instance.workflowState?.incidents?.map((i) => i.activityId) ?? [])),
          };
          useAlterationDialogs.getState().openAlter([target], helpers.refresh);
        },
      },
    ],
  },
});
