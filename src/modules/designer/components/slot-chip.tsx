"use client";

import { ChevronRight, Plus, Trash2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import type { Activity } from "@/lib/api/types/activities";
import { useDesigner } from "../context";
import { activityDisplayText } from "../model/display";
import { activityAccentColor } from "../model/icons";
import type { SlotPath } from "../model/containers";
import { ActivityIcon } from "./activity-icon";

/**
 * Editor for one child slot (state entry/exit, transition trigger/action, embedded port): shows the
 * activity in it with open / edit / remove, or a button that opens the quick-add palette to fill it.
 */
export function SlotChip({
  ownerId,
  slot,
  child,
  label,
  hint,
  readOnly,
  triggersOnly,
  allowContainers = true,
}: {
  ownerId: string;
  slot: SlotPath;
  child: Activity | null | undefined;
  label: string;
  hint?: string;
  readOnly?: boolean;
  triggersOnly?: boolean;
  allowContainers?: boolean;
}) {
  const { store, registry } = useDesigner();
  const descriptor = child ? registry.get(child.type, child.version) : undefined;
  const isContainer = !!child && ["Elsa.Flowchart", "Elsa.Sequence", "Elsa.StateMachine"].includes(child.type);
  const accent = activityAccentColor(descriptor);
  return (
    <div className="flex flex-col gap-1">
      <div className="flex items-center justify-between">
        <span className="text-sm font-medium">{label}</span>
        {child && !readOnly && (
          <Button variant="ghost" size="icon-xs" aria-label={`Remove ${label}`} className="text-muted-foreground hover:text-status-danger" onClick={() => store.getState().fillSlot(ownerId, slot, undefined)}>
            <Trash2 />
          </Button>
        )}
      </div>
      {child ? (
        <button
          type="button"
          className="flex w-full items-center gap-2 rounded-md border bg-card px-2 py-1.5 text-left hover:border-primary"
          onClick={() => (isContainer ? store.getState().enterSlot(ownerId, slot) : store.getState().select([child.id]))}
          title={isContainer ? "Open" : "Edit properties"}
        >
          <span className="flex size-6 shrink-0 items-center justify-center rounded-md" style={{ background: `color-mix(in oklab, ${accent} 14%, var(--card))`, color: accent }}>
            <ActivityIcon typeName={child.type} descriptor={descriptor} className="size-3.5" />
          </span>
          <span className="min-w-0 flex-1">
            <span className="block truncate text-sm">{activityDisplayText(child, descriptor)}</span>
            <span className="block truncate text-[11px] text-muted-foreground">{descriptor?.displayName ?? child.type}</span>
          </span>
          <ChevronRight className="size-4 text-muted-foreground" />
        </button>
      ) : readOnly ? (
        <span className="text-sm text-muted-foreground">none</span>
      ) : (
        <Button
          variant="outline"
          size="sm"
          className="justify-start border-dashed text-muted-foreground"
          onClick={(e) => store.getState().openQuickAdd({ screen: { x: e.clientX - 300, y: e.clientY + 8 }, slot: { activityId: ownerId, slot }, allowContainers: allowContainers && !triggersOnly, triggersOnly })}
        >
          <Plus /> {triggersOnly ? "Choose trigger" : "Add activity"}
        </Button>
      )}
      {hint && <p className="text-xs text-muted-foreground">{hint}</p>}
    </div>
  );
}
