"use client";

import { createContext, useContext, useMemo, type ReactNode } from "react";
import { useStudioConfig } from "@/config/provider";
import { useRemoteFeatures } from "@/lib/features/remote-features";
import { usePermissions } from "@/lib/permissions";
import type { NavItem, StudioModule } from "./types";

interface ModuleRegistryValue {
  /** All modules compiled into the app. */
  all: StudioModule[];
  /** Modules enabled by configuration and available on the current backend. */
  enabled: StudioModule[];
  /** True once the server feature catalog has been fetched (or failed). */
  featuresResolved: boolean;
}

const ModuleRegistryContext = createContext<ModuleRegistryValue | null>(null);

export function ModuleRegistryProvider({ modules, children }: { modules: StudioModule[]; children: ReactNode }) {
  const config = useStudioConfig();
  const { catalog, isResolved, isError } = useRemoteFeatures();

  const enabled = useMemo(
    () =>
      modules.filter((m) => {
        if (!m.core && !(config.features[m.id]?.enabled ?? false)) return false;
        if (!m.requiresServerFeatures?.length) return true;
        // When the catalog can't be read (older backend, 404) fall back to the configuration flag alone.
        if (isError) return true;
        return m.requiresServerFeatures.every((f) => catalog.has(f));
      }),
    [modules, config.features, catalog, isError],
  );

  const value = useMemo(() => ({ all: modules, enabled, featuresResolved: isResolved }), [modules, enabled, isResolved]);
  return <ModuleRegistryContext.Provider value={value}>{children}</ModuleRegistryContext.Provider>;
}

export function useModules(): ModuleRegistryValue {
  const ctx = useContext(ModuleRegistryContext);
  if (!ctx) throw new Error("useModules must be used inside <ModuleRegistryProvider>");
  return ctx;
}

/**
 * Navigation tree aggregated from enabled modules, filtered by permission and sorted. Items that
 * share an id are merged (their children concatenated) so several modules can contribute to one
 * parent such as "Workflows".
 */
export function useNavigation(): NavItem[] {
  const { enabled } = useModules();
  const permissions = usePermissions();
  return useMemo(() => {
    const merge = (items: NavItem[]): NavItem[] => {
      const byId = new Map<string, NavItem>();
      for (const item of items) {
        const existing = byId.get(item.id);
        if (!existing) {
          byId.set(item.id, { ...item, children: item.children ? [...item.children] : undefined });
          continue;
        }
        byId.set(item.id, {
          ...existing,
          ...Object.fromEntries(Object.entries(item).filter(([, v]) => v !== undefined)),
          children: existing.children || item.children ? [...(existing.children ?? []), ...(item.children ?? [])] : undefined,
        });
      }
      return [...byId.values()].map((i) => (i.children ? { ...i, children: merge(i.children) } : i));
    };
    const visible = (item: NavItem): NavItem | undefined => {
      if (item.permissions && !permissions.hasAny(item.permissions)) return undefined;
      const children = item.children?.map(visible).filter((c): c is NavItem => !!c);
      if (item.children && children?.length === 0) return undefined;
      return { ...item, children: children?.sort(byOrder) };
    };
    return merge(enabled.flatMap((m) => m.nav ?? []))
      .map(visible)
      .filter((i): i is NavItem => !!i)
      .sort(byOrder);
  }, [enabled, permissions]);
}

function byOrder(a: { order?: number }, b: { order?: number }) {
  return (a.order ?? 0) - (b.order ?? 0);
}

/** Collects one extension kind across enabled modules, e.g. `useExtensions("dashboardWidgets")`. */
export function useExtensions<K extends keyof NonNullable<StudioModule["extensions"]>>(
  kind: K,
): NonNullable<NonNullable<StudioModule["extensions"]>[K]> {
  const { enabled } = useModules();
  return useMemo(() => {
    const items = enabled.map((m) => m.extensions?.[kind]).filter(Boolean);
    if (kind === "uiHints") return Object.assign({}, ...items) as never;
    return (items.flat() as { order?: number }[]).sort(byOrder) as never;
  }, [enabled, kind]);
}
