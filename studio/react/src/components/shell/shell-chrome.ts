"use client";

import { useEffect, type ReactNode } from "react";
import { create } from "zustand";

/**
 * Lets a page take over the app header (e.g. the workflow editor renders a breadcrumb and its
 * own actions there instead of stacking a second toolbar) and drop the content padding.
 */
interface ShellChromeState {
  header: ReactNode | null;
  fullBleed: boolean;
  set: (patch: Partial<Pick<ShellChromeState, "header" | "fullBleed">>) => void;
}

export const useShellChrome = create<ShellChromeState>()((set) => ({
  header: null,
  fullBleed: false,
  set: (patch) => set(patch),
}));

/** Replaces the header while the calling component is mounted. */
export function useShellHeader(header: ReactNode, options: { fullBleed?: boolean } = {}) {
  const set = useShellChrome((s) => s.set);
  const fullBleed = options.fullBleed ?? false;
  useEffect(() => {
    set({ header, fullBleed });
    return () => set({ header: null, fullBleed: false });
  }, [header, fullBleed, set]);
}
