"use client";

import { createContext, useContext, useMemo, type ReactNode } from "react";
import { useStore } from "zustand";
import type { DesignerState, DesignerStore } from "./store";
import { ActivityRegistry } from "./model/registry";
import type { ActivityExecutionStats } from "@/lib/api/types/workflow-instances";
import type { DataFlowIndex } from "./model/data-flow";
import type { Activity } from "@/lib/api/types/activities";

/** A navigation target an activity points at (e.g. the workflow a Dispatch Workflow step calls). */
export interface ActivityLink {
  label: string;
  onOpen: () => void;
}

export interface DesignerContextValue {
  store: DesignerStore;
  registry: ActivityRegistry;
  /** Execution statistics per activity node id (instance viewer). */
  stats?: Map<string, ActivityExecutionStats>;
  /** Variable / input / output usage index (editor). */
  dataFlow?: DataFlowIndex;
  /** Resolves a link for an activity, shown as an "open" action on the step; undefined when none. */
  activityLink?: (activity: Activity) => ActivityLink | undefined;
}

const DesignerContext = createContext<DesignerContextValue | null>(null);

export function DesignerProvider({
  store,
  registry,
  stats,
  dataFlow,
  activityLink,
  children,
}: {
  store: DesignerStore;
  registry: ActivityRegistry;
  stats?: Map<string, ActivityExecutionStats>;
  dataFlow?: DataFlowIndex;
  activityLink?: (activity: Activity) => ActivityLink | undefined;
  children: ReactNode;
}) {
  const value = useMemo(() => ({ store, registry, stats, dataFlow, activityLink }), [store, registry, stats, dataFlow, activityLink]);
  return <DesignerContext.Provider value={value}>{children}</DesignerContext.Provider>;
}

export function useDesigner(): DesignerContextValue {
  const ctx = useContext(DesignerContext);
  if (!ctx) throw new Error("useDesigner must be used inside <DesignerProvider>");
  return ctx;
}

export function useDesignerState<T>(selector: (s: DesignerState) => T): T {
  return useStore(useDesigner().store, selector);
}
