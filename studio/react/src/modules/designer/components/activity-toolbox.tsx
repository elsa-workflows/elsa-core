"use client";

import { useMemo, useState, type DragEvent } from "react";
import { useReactFlow } from "@xyflow/react";
import { ChevronDown, ChevronRight, GripVertical, PanelLeftClose } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip";
import { cn } from "@/lib/utils";
import type { ActivityDescriptor } from "@/lib/api/types/descriptors";
import { useDesigner } from "../context";
import { activityAccentColor } from "../model/icons";
import { ActivityIcon } from "./activity-icon";

export const ACTIVITY_DRAG_TYPE = "application/x-elsa-activity";

export function setActivityDragData(e: DragEvent, descriptor: ActivityDescriptor) {
  e.dataTransfer.setData(ACTIVITY_DRAG_TYPE, JSON.stringify({ typeName: descriptor.typeName, version: descriptor.version }));
  e.dataTransfer.effectAllowed = "copy";
}

export function readActivityDragData(e: DragEvent): { typeName: string; version: number } | undefined {
  const raw = e.dataTransfer.getData(ACTIVITY_DRAG_TYPE);
  if (!raw) return undefined;
  try {
    return JSON.parse(raw) as { typeName: string; version: number };
  } catch {
    return undefined;
  }
}

/** Activity palette grouped by category; items can be dragged onto the canvas or clicked to add at the viewport centre. */
export function ActivityToolbox({ className, onCollapse }: { className?: string; onCollapse?: () => void }) {
  const { registry, store } = useDesigner();
  const [search, setSearch] = useState("");
  // Categories start collapsed so the toolbox reads as a short index; a search expands the matches.
  const [expanded, setExpanded] = useState<Record<string, boolean>>({});
  const flow = useReactFlow();

  const groups = useMemo(() => {
    const term = search.trim().toLowerCase();
    const map = new Map<string, ActivityDescriptor[]>();
    for (const d of registry.browsable) {
      if (term && !`${d.displayName} ${d.typeName} ${d.description ?? ""} ${d.category}`.toLowerCase().includes(term)) continue;
      const list = map.get(d.category) ?? [];
      list.push(d);
      map.set(d.category, list);
    }
    return [...map.entries()]
      .sort(([a], [b]) => a.localeCompare(b))
      .map(([category, items]) => ({ category, items: items.sort((a, b) => a.displayName.localeCompare(b.displayName)) }));
  }, [registry, search]);

  const addAtCenter = (descriptor: ActivityDescriptor) => {
    const el = document.querySelector<HTMLElement>(".react-flow");
    const rect = el?.getBoundingClientRect();
    const center = flow.screenToFlowPosition({ x: (rect?.left ?? 0) + (rect?.width ?? 800) / 2, y: (rect?.top ?? 0) + (rect?.height ?? 600) / 2 });
    // Place the new node to the right of the right-most existing node so additions never overlap.
    const existing = store.getState().children();
    const rightMost = existing.reduce((max, a) => {
      const pos = a.metadata?.designer?.position;
      const width = a.metadata?.designer?.size?.width ?? 220;
      return pos ? Math.max(max, pos.x + width) : max;
    }, Number.NEGATIVE_INFINITY);
    const anchorY = existing.length ? (existing[existing.length - 1].metadata?.designer?.position?.y ?? center.y) : center.y - 28;
    const x = Number.isFinite(rightMost) ? rightMost + 80 : center.x - 110;
    store.getState().addActivity(descriptor, { x: Math.round(x), y: Math.round(anchorY) });
  };

  return (
    <div className={cn("flex h-full min-h-0 flex-col", className)}>
      <div className="flex items-center gap-1 p-2">
        <Input placeholder="Search activities…" value={search} onChange={(e) => setSearch(e.target.value)} className="h-8 bg-background" />
        {onCollapse && (
          <Tooltip>
            <TooltipTrigger render={<Button variant="ghost" size="icon-sm" aria-label="Hide toolbox" onClick={onCollapse} className="shrink-0 text-muted-foreground" />}>
              <PanelLeftClose />
            </TooltipTrigger>
            <TooltipContent>Hide toolbox</TooltipContent>
          </Tooltip>
        )}
      </div>
      <div className="min-h-0 flex-1 overflow-y-auto px-1 pb-2">
        {groups.length === 0 && <p className="px-2 py-4 text-xs text-muted-foreground">No activities match.</p>}
        {groups.map(({ category, items }) => {
          const isCollapsed = !search && !expanded[category];
          return (
            <div key={category} className="mb-1">
              <button
                type="button"
                className="eyebrow flex w-full items-center gap-1 rounded px-2 py-1.5 hover:bg-muted"
                onClick={() => setExpanded((c) => ({ ...c, [category]: !c[category] }))}
              >
                {isCollapsed ? <ChevronRight className="size-3" /> : <ChevronDown className="size-3" />}
                {category}
                <span className="ml-auto font-normal">{items.length}</span>
              </button>
              {!isCollapsed &&
                items.map((d) => {
                  return (
                    <Tooltip key={`${d.typeName}:${d.version}`}>
                      <TooltipTrigger
                        render={
                          <div
                            draggable
                            onDragStart={(e) => setActivityDragData(e, d)}
                            onDoubleClick={() => addAtCenter(d)}
                            className="group flex cursor-grab items-center gap-2 rounded px-2 py-1 text-sm hover:bg-muted active:cursor-grabbing"
                          />
                        }
                      >
                        <GripVertical className="size-3 text-muted-foreground opacity-0 group-hover:opacity-100" />
                        <span className="flex size-6 shrink-0 items-center justify-center rounded-md" style={{ background: `color-mix(in oklab, ${activityAccentColor(d)} 14%, var(--card))`, color: activityAccentColor(d) }}>
                          <ActivityIcon typeName={d.typeName} descriptor={d} className="size-3.5" />
                        </span>
                        <span className="truncate">{d.displayName}</span>
                      </TooltipTrigger>
                      <TooltipContent side="right" className="max-w-xs">
                        <div className="font-medium">{d.displayName}</div>
                        {d.description && <div className="text-xs opacity-80">{d.description}</div>}
                        <div className="mt-1 text-[10px] opacity-60">{d.typeName} v{d.version} · drag onto the canvas or double-click</div>
                      </TooltipContent>
                    </Tooltip>
                  );
                })}
            </div>
          );
        })}
      </div>
      <div className="border-t px-3 py-1.5 text-[11px] text-muted-foreground">Drag onto the canvas or double-click to add</div>
    </div>
  );
}
