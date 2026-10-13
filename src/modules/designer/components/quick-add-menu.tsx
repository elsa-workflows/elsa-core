"use client";

import { useEffect, useMemo, useRef, useState } from "react";
import { createPortal } from "react-dom";
import { Command, CommandEmpty, CommandGroup, CommandInput, CommandItem, CommandList } from "@/components/ui/command";
import type { ActivityDescriptor } from "@/lib/api/types/descriptors";
import { useDesigner, useDesignerState } from "../context";
import type { DesignerState } from "../store";
import { activityAccentColor } from "../model/icons";
import { readRecentActivities, rememberActivity } from "../model/recent";
import { ActivityIcon } from "./activity-icon";

const WIDTH = 340;
const HEIGHT = 420;

/**
 * Searchable activity palette anchored at the point the user clicked (a node's "+", a connection,
 * the empty canvas). Picking an item places and wires the activity in one step.
 */
export function QuickAddMenu() {
  const { registry, store } = useDesigner();
  const request = useDesignerState((s) => s.quickAdd);
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!request) return;
    const onDown = (e: MouseEvent) => {
      if (ref.current && !ref.current.contains(e.target as Node)) store.getState().closeQuickAdd();
    };
    const onKey = (e: KeyboardEvent) => {
      if (e.key === "Escape") store.getState().closeQuickAdd();
    };
    document.addEventListener("mousedown", onDown);
    document.addEventListener("keydown", onKey);
    return () => {
      document.removeEventListener("mousedown", onDown);
      document.removeEventListener("keydown", onKey);
    };
  }, [request, store]);

  const recent = useMemo(() => {
    if (!request) return [];
    return readRecentActivities().map((t) => registry.get(t)).filter((d): d is ActivityDescriptor => !!d);
  }, [request, registry]);

  const groups = useMemo(() => {
    const map = new Map<string, ActivityDescriptor[]>();
    const source = request?.triggersOnly ? registry.browsable.filter((d) => d.kind === "Trigger") : registry.browsable;
    for (const d of source) map.set(d.category, [...(map.get(d.category) ?? []), d]);
    const sorted = [...map.entries()].sort(([a], [b]) => a.localeCompare(b));
    if (request?.allowContainers) {
      const containers = ["Elsa.Flowchart", "Elsa.Sequence", "Elsa.StateMachine"].map((t) => registry.get(t)).filter((d): d is ActivityDescriptor => !!d);
      if (containers.length) sorted.unshift(["Containers", containers]);
    }
    return sorted;
  }, [registry, request?.allowContainers, request?.triggersOnly]);

  if (!request || typeof document === "undefined") return null;

  const pick = (d: ActivityDescriptor) => {
    rememberActivity(d.typeName);
    store.getState().quickAddActivity(d, request);
  };

  const left = Math.max(8, Math.min(request.screen.x, window.innerWidth - WIDTH - 8));
  const top = Math.max(8, Math.min(request.screen.y, window.innerHeight - HEIGHT - 8));
  const sourceName = request.source ? store.getState().children().find((a) => a.id === request.source!.activityId)?.name : undefined;

  return createPortal(
    <QuickAddPanel key={`${request.screen.x}:${request.screen.y}:${request.source?.activityId ?? ""}:${request.insertInto ? "insert" : ""}:${request.slot?.activityId ?? ""}:${request.slot?.slot ?? ""}:${request.sequenceIndex ?? ""}`} request={request} left={left} top={top} sourceName={sourceName} recent={recent} groups={groups} pick={pick} panelRef={ref} />,
    document.body,
  );
}

function QuickAddPanel({
  request,
  left,
  top,
  sourceName,
  recent,
  groups,
  pick,
  panelRef,
}: {
  request: NonNullable<ReturnType<typeof useDesignerState<DesignerState["quickAdd"]>>>;
  left: number;
  top: number;
  sourceName?: string;
  recent: ActivityDescriptor[];
  groups: [string, ActivityDescriptor[]][];
  pick: (d: ActivityDescriptor) => void;
  panelRef: React.RefObject<HTMLDivElement | null>;
}) {
  const [query, setQuery] = useState("");
  return (
    <div
      ref={panelRef}
      className="fixed z-[100] overflow-hidden rounded-lg border bg-popover text-popover-foreground shadow-xl"
      style={{ left, top, width: WIDTH, height: HEIGHT }}
      role="dialog"
      aria-label="Add activity"
    >
      <Command loop className="h-full" filter={rankMatch}>
        <CommandInput
          autoFocus
          value={query}
          onValueChange={setQuery}
          placeholder={
            request.slot
              ? request.triggersOnly
                ? "Choose a trigger…"
                : "Choose an activity or container…"
              : request.source
                ? `Add after ${sourceName ?? "activity"} (${request.source.port})…`
                : request.insertInto
                  ? "Insert activity here…"
                  : "Add activity…"
          }
        />
        <CommandList className="max-h-none h-[calc(100%-2.75rem)]">
          <CommandEmpty>No activities match.</CommandEmpty>
          {!query && recent.length > 0 && (
            <CommandGroup heading="Recently used">
              {recent.map((d) => (
                <Item key={`recent-${d.typeName}`} d={d} onSelect={() => pick(d)} />
              ))}
            </CommandGroup>
          )}
          {groups.map(([category, items]) => (
            <CommandGroup key={category} heading={category}>
              {items.map((d) => (
                <Item key={d.typeName} d={d} onSelect={() => pick(d)} />
              ))}
            </CommandGroup>
          ))}
        </CommandList>
      </Command>
    </div>
  );
}

/**
 * Deterministic ranking instead of cmdk's fuzzy default: word-prefix matches on the display name win,
 * then substring matches on name/type, then category/description. Anything else is hidden.
 */
function rankMatch(value: string, search: string): number {
  const q = search.trim().toLowerCase();
  if (!q) return 1;
  const [name, type, category, ...rest] = value.split("\u0000");
  const n = name.toLowerCase();
  if (n === q) return 1;
  if (n.startsWith(q)) return 0.95;
  if (n.split(/\s+/).some((w) => w.startsWith(q)) || n.replace(/\s+/g, "").startsWith(q.replace(/\s+/g, ""))) return 0.9;
  if (n.includes(q) || n.replace(/\s+/g, "").includes(q.replace(/\s+/g, ""))) return 0.8;
  if (type.toLowerCase().includes(q.replace(/\s+/g, ""))) return 0.6;
  if (category.toLowerCase().includes(q)) return 0.4;
  if (rest.join(" ").toLowerCase().includes(q)) return 0.2;
  return 0;
}

function Item({ d, onSelect }: { d: ActivityDescriptor; onSelect: () => void }) {
  const accent = activityAccentColor(d);
  return (
    <CommandItem value={[d.displayName, d.typeName, d.category, d.description ?? ""].join("\u0000")} keywords={[d.displayName]} onSelect={onSelect} className="gap-2.5">
      <span className="flex size-6 shrink-0 items-center justify-center rounded-md" style={{ background: `color-mix(in oklab, ${accent} 14%, var(--card))`, color: accent }}>
        <ActivityIcon typeName={d.typeName} descriptor={d} className="size-3.5" />
      </span>
      <span className="flex min-w-0 flex-col">
        <span className="truncate text-sm">{d.displayName}</span>
        {d.description && <span className="truncate text-[11px] text-muted-foreground">{d.description}</span>}
      </span>
    </CommandItem>
  );
}
