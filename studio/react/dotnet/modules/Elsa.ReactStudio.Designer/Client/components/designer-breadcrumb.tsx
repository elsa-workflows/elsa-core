"use client";
import { useTranslation } from "@elsa-workflows/react-studio-sdk/localization";


import { Fragment } from "react";
import { ChevronRight, Home } from "lucide-react";
import { Button } from "@elsa-workflows/react-studio-ui/button";
import { useDesigner, useDesignerState } from "@elsa-workflows/react-studio-sdk/designer/context";
import { resolveContainer } from "@elsa-workflows/react-studio-sdk/designer/model/flowchart-ops";
import { activityDisplayText } from "@elsa-workflows/react-studio-sdk/designer/model/display";
import { childActivities, listSlots } from "@elsa-workflows/react-studio-sdk/designer/model/containers";

/** Shows the nesting path (root → If1: Then → …) and lets the user jump back up. */
export function DesignerBreadcrumb() {
  const _t = useTranslation();
  const { registry } = useDesigner();
  const root = useDesignerState((s) => s.root);
  const path = useDesignerState((s) => s.path);
  const navigateTo = useDesignerState((s) => s.navigateTo);

  const crumbs = path.map((seg, i) => {
    const container = resolveContainer(root, path.slice(0, i));
    const activity = container?.id === seg.activityId ? container : childActivities(container ?? root).find((a) => a.id === seg.activityId);
    const descriptor = activity ? registry.get(activity.type, activity.version) : undefined;
    const name = activity ? activityDisplayText(activity, descriptor) : seg.activityId;
    const slotLabel = activity && seg.slot ? (listSlots(activity, descriptor).find((s) => s.slot === seg.slot)?.label ?? seg.slot) : undefined;
    return { label: slotLabel ? `${name}: ${slotLabel}` : name, depth: i + 1 };
  });

  if (path.length === 0) return null;

  return (
    <div className="flex items-center gap-0.5 rounded-lg border bg-card/95 px-1 py-0.5 text-xs shadow-sm backdrop-blur">
      <Button variant="ghost" size="xs" onClick={() => navigateTo(0)} disabled={path.length === 0} aria-label={_t("Root")}>
        <Home className="size-3" />
        {_t("Root")}</Button>
      {crumbs.map((c) => (
        <Fragment key={c.depth}>
          <ChevronRight className="size-3 text-muted-foreground" />
          <Button variant="ghost" size="xs" onClick={() => navigateTo(c.depth)} disabled={c.depth === path.length}>
            {c.label}
          </Button>
        </Fragment>
      ))}
    </div>
  );
}
