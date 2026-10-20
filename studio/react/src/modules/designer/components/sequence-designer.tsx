"use client";

import { useMemo, useRef, useState, type DragEvent } from "react";
import { ArrowDown, GripVertical, MousePointerClick, Plus } from "lucide-react";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import { isSequence, type Activity, type Sequence } from "@/lib/api/types/activities";
import { useDesigner, useDesignerState } from "../context";
import { resolveContainer } from "../model/flowchart-ops";
import { computePorts } from "../model/ports";
import { ActivityCard } from "./activity-card";
import { DesignerBreadcrumb } from "./designer-breadcrumb";
import { QuickAddMenu } from "./quick-add-menu";
import { readActivityDragData } from "./activity-toolbox";

/**
 * Designer for `Elsa.Sequence`: children run top to bottom, so the surface is an ordered list with
 * implicit arrows. There are no manual connections; order is the only edge information, changed by
 * dragging a card or with the insert buttons. A terminal step (End, Finish) ends the sequence early.
 */
export function SequenceDesigner() {
  const { store, registry, stats } = useDesigner();
  const root = useDesignerState((s) => s.root);
  const path = useDesignerState((s) => s.path);
  const selectedIds = useDesignerState((s) => s.selectedIds);
  const readOnly = useDesignerState((s) => s.readOnly);
  const container = useMemo(() => {
    const c = resolveContainer(root, path);
    return isSequence(c) ? c : undefined;
  }, [root, path]);
  const [dragIndex, setDragIndex] = useState<number | null>(null);
  const [overIndex, setOverIndex] = useState<number | null>(null);
  const listRef = useRef<HTMLDivElement>(null);

  if (!container) return null;
  const items = container.activities;

  const openAdd = (e: React.MouseEvent, index: number) => {
    store.getState().openQuickAdd({ screen: { x: e.clientX - 170, y: e.clientY + 12 }, sequenceIndex: index, allowContainers: true });
  };

  const onDropFromToolbox = (e: DragEvent, index: number) => {
    const data = readActivityDragData(e);
    if (!data || readOnly) return;
    e.preventDefault();
    const descriptor = registry.get(data.typeName, data.version);
    if (!descriptor) return;
    store.getState().quickAddActivity(descriptor, { screen: { x: 0, y: 0 }, sequenceIndex: index });
  };

  return (
    <div className="relative flex h-full min-h-0 flex-col overflow-auto bg-canvas" onClick={() => store.getState().select([])}>
      <div className="sticky top-0 z-10 flex items-center gap-2 p-3">
        <DesignerBreadcrumb />
        <span className="ml-auto rounded-full border bg-card/95 px-2 py-0.5 text-[11px] text-muted-foreground shadow-sm">Sequence · runs top to bottom</span>
      </div>
      <div ref={listRef} className="mx-auto flex w-full max-w-md flex-col items-stretch px-4 pb-16 pt-2" onClick={(e) => e.stopPropagation()}>
        {items.length === 0 && (
          <div className="flex flex-col items-center gap-3 rounded-xl border border-dashed bg-card/95 p-6 text-center shadow-sm">
            <span className="flex size-12 items-center justify-center rounded-xl bg-accent text-primary">
              <MousePointerClick className="size-6" />
            </span>
            <div className="text-sm font-semibold">Add the first step</div>
            <p className="text-xs text-muted-foreground">Steps run one after another. Use the + buttons to insert, drag cards to reorder.</p>
            {!readOnly && (
              <Button size="sm" onClick={(e) => openAdd(e, 0)}>
                <Plus /> Add step
              </Button>
            )}
          </div>
        )}
        {items.map((activity, index) => (
          <SequenceRow
            key={activity.id}
            container={container}
            activity={activity}
            index={index}
            selected={selectedIds.includes(activity.id)}
            readOnly={readOnly}
            isDragOver={overIndex === index && dragIndex !== null && dragIndex !== index}
            onDragStart={() => setDragIndex(index)}
            onDragOver={(e) => {
              e.preventDefault();
              setOverIndex(index);
            }}
            onDrop={(e) => {
              if (dragIndex !== null) {
                store.getState().moveChild(dragIndex, index);
              } else {
                onDropFromToolbox(e, index);
              }
              setDragIndex(null);
              setOverIndex(null);
            }}
            onDragEnd={() => {
              setDragIndex(null);
              setOverIndex(null);
            }}
            onInsertBefore={(e) => openAdd(e, index)}
            onInsertAfter={(e) => openAdd(e, index + 1)}
            stats={stats}
            registryGet={(a) => registry.get(a.type, a.version)}
            isLast={index === items.length - 1}
          />
        ))}
        {items.length > 0 && !readOnly && (
          <div className="flex justify-center pt-1" onDragOver={(e) => e.preventDefault()} onDrop={(e) => onDropFromToolbox(e, items.length)}>
            <Button variant="outline" size="sm" onClick={(e) => openAdd(e, items.length)}>
              <Plus /> Add step
            </Button>
          </div>
        )}
      </div>
      <QuickAddMenu />
    </div>
  );
}

function SequenceRow({
  container,
  activity,
  index,
  selected,
  readOnly,
  isDragOver,
  onDragStart,
  onDragOver,
  onDrop,
  onDragEnd,
  onInsertBefore,
  onInsertAfter,
  stats,
  registryGet,
  isLast,
}: {
  container: Sequence;
  activity: Activity;
  index: number;
  selected: boolean;
  readOnly: boolean;
  isDragOver: boolean;
  onDragStart: () => void;
  onDragOver: (e: DragEvent) => void;
  onDrop: (e: DragEvent) => void;
  onDragEnd: () => void;
  onInsertBefore: (e: React.MouseEvent) => void;
  onInsertAfter: (e: React.MouseEvent) => void;
  stats: ReturnType<typeof useDesigner>["stats"];
  registryGet: (a: Activity) => ReturnType<ReturnType<typeof useDesigner>["registry"]["get"]>;
  isLast: boolean;
}) {
  const { store } = useDesigner();
  const descriptor = registryGet(activity);
  const terminal = descriptor?.isTerminal === true;
  void container;
  void computePorts;
  return (
    <div className="group/row flex flex-col items-center">
      {index === 0 && !readOnly && <InsertButton onClick={onInsertBefore} />}
      <div
        className={cn("relative w-full transition-transform", isDragOver && "translate-y-1")}
        draggable={!readOnly}
        onDragStart={(e) => {
          e.dataTransfer.effectAllowed = "move";
          onDragStart();
        }}
        onDragOver={onDragOver}
        onDrop={onDrop}
        onDragEnd={onDragEnd}
      >
        {isDragOver && <div className="absolute -top-1.5 left-0 right-0 h-0.5 rounded bg-primary" />}
        {!readOnly && (
          <span className="absolute -left-6 top-1/2 -translate-y-1/2 cursor-grab text-muted-foreground opacity-0 transition-opacity group-hover/row:opacity-100" title="Drag to reorder">
            <GripVertical className="size-4" />
          </span>
        )}
        <ActivityCard
          activity={activity}
          descriptor={descriptor}
          selected={selected}
          readOnly={readOnly}
          stats={stats?.get(activity.nodeId ?? activity.id)}
          badge={<span className="rounded-full bg-muted px-1.5 text-[10px] font-semibold tabular-nums text-muted-foreground">{index + 1}</span>}
          onSelect={() => store.getState().select([activity.id])}
        />
      </div>
      {!isLast ? (
        <div className="relative flex h-10 flex-col items-center justify-center">
          <span className={cn("h-full w-px", terminal ? "border-l border-dashed border-status-danger/60" : "bg-muted-foreground/40")} />
          <ArrowDown className={cn("absolute bottom-0 size-3.5", terminal ? "text-status-danger/60" : "text-muted-foreground/70")} />
          {!readOnly && <InsertButton onClick={onInsertAfter} floating />}
        </div>
      ) : (
        !readOnly && <div className="h-2" />
      )}
    </div>
  );
}

function InsertButton({ onClick, floating }: { onClick: (e: React.MouseEvent) => void; floating?: boolean }) {
  return (
    <button
      type="button"
      onClick={onClick}
      title="Insert step here"
      aria-label="Insert step here"
      className={cn(
        "flex size-5 items-center justify-center rounded-full border bg-card text-muted-foreground shadow-sm transition-opacity hover:border-primary hover:text-primary",
        floating ? "absolute left-1/2 top-1/2 -translate-x-1/2 -translate-y-1/2 opacity-0 group-hover/row:opacity-100" : "mb-2",
      )}
    >
      <Plus className="size-3" />
    </button>
  );
}
