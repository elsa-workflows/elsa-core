"use client";

import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { useCallback } from "react";

/**
 * Reads and writes list state (page, search, filters) in the query string so links are shareable
 * and the browser back button restores the view.
 */
export function useUrlState() {
  const router = useRouter();
  const pathname = usePathname();
  const params = useSearchParams();

  const get = useCallback((key: string) => params.get(key) ?? undefined, [params]);
  const getAll = useCallback((key: string) => params.getAll(key), [params]);
  const getNumber = useCallback(
    (key: string, fallback: number) => {
      const v = params.get(key);
      const n = v === null ? NaN : Number(v);
      return Number.isFinite(n) ? n : fallback;
    },
    [params],
  );

  const set = useCallback(
    (updates: Record<string, string | number | string[] | undefined | null>) => {
      const next = new URLSearchParams(params.toString());
      for (const [k, v] of Object.entries(updates)) {
        next.delete(k);
        if (v === undefined || v === null || v === "") continue;
        if (Array.isArray(v)) v.forEach((x) => next.append(k, x));
        else next.set(k, String(v));
      }
      const qs = next.toString();
      router.replace(qs ? `${pathname}?${qs}` : pathname, { scroll: false });
    },
    [params, pathname, router],
  );

  return { get, getAll, getNumber, set };
}
