"use client";

import { useCallback, useSyncExternalStore } from "react";

/**
 * There is no "list plans" endpoint in `Elsa.Alterations`; the studio remembers the plans it
 * submitted in `localStorage` (per server) so they can be revisited on `/alterations/plans`.
 */
export interface RecentPlanRef {
  id: string;
  submittedAt: string;
  instanceCount?: number;
  alterationCount: number;
}

const MAX_RECENT = 50;
const listeners = new Set<() => void>();
const cache = new Map<string, RecentPlanRef[]>();
const EMPTY: RecentPlanRef[] = [];

function storageKey(scope: string) {
  return `elsa-studio:alterations:recent-plans:${scope}`;
}

function read(scope: string): RecentPlanRef[] {
  const cached = cache.get(scope);
  if (cached) return cached;
  let value: RecentPlanRef[] = EMPTY;
  try {
    const raw = typeof window !== "undefined" ? window.localStorage.getItem(storageKey(scope)) : null;
    if (raw) {
      const parsed: unknown = JSON.parse(raw);
      if (Array.isArray(parsed)) value = parsed.filter((p): p is RecentPlanRef => typeof p === "object" && p !== null && typeof (p as RecentPlanRef).id === "string");
    }
  } catch {
    value = EMPTY;
  }
  cache.set(scope, value);
  return value;
}

function write(scope: string, next: RecentPlanRef[]) {
  cache.set(scope, next);
  try {
    window.localStorage.setItem(storageKey(scope), JSON.stringify(next));
  } catch {
    // Storage may be unavailable (private mode); the in-memory cache still works for this session.
  }
  listeners.forEach((l) => l());
}

function subscribe(listener: () => void) {
  listeners.add(listener);
  return () => listeners.delete(listener);
}

export function useRecentPlans(scope: string) {
  const plans = useSyncExternalStore(subscribe, () => read(scope), () => EMPTY);

  const remember = useCallback(
    (plan: RecentPlanRef) => {
      const existing = read(scope).filter((p) => p.id !== plan.id);
      write(scope, [plan, ...existing].slice(0, MAX_RECENT));
    },
    [scope],
  );

  const forget = useCallback(
    (id: string) => {
      write(
        scope,
        read(scope).filter((p) => p.id !== id),
      );
    },
    [scope],
  );

  const clear = useCallback(() => write(scope, []), [scope]);

  return { plans, remember, forget, clear };
}
