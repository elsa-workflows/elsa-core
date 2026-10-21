import { Tag } from "lucide-react";
import { defineModule, NavGroups } from "@/lib/modules/types";
import { Resources, Verbs } from "@/lib/permissions";
import { WorkflowLabelsTab } from "./components/workflow-labels-tab";
import { LabelsPage } from "./pages/labels-page";

export const LabelRoutes = { list: "/labels" } as const;

export { LabelBadge, LabelSwatch } from "./components/label-badge";

export const labelsModule = defineModule({
  id: "labels",
  name: "Labels",
  description: "Tag workflow definitions with coloured labels.",
  requiresServerFeatures: ["Elsa.Labels"],
  routes: [{ path: LabelRoutes.list, component: LabelsPage, title: "Labels", permissions: [{ resource: Resources.labels, verb: Verbs.view }] }],
  nav: [
    {
      id: "labels",
      label: "Labels",
      href: LabelRoutes.list,
      icon: Tag,
      group: NavGroups.administration.id,
      order: 200,
      permissions: [{ resource: Resources.labels, verb: Verbs.view }],
    },
  ],
  extensions: {
    workflowPropertiesTabs: [{ id: "labels", label: "Labels", order: 50, component: WorkflowLabelsTab }],
  },
});
