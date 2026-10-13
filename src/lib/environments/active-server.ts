"use client";

import { create } from "zustand";
import { persist } from "zustand/middleware";
import type { ServerConfig } from "@/config/schema";
import { useStudioConfig } from "@/config/provider";

interface ActiveServerState {
  /** Index into `config.servers`. Persisted so a reload keeps the chosen environment. */
  index: number;
  setIndex: (index: number) => void;
}

export const useActiveServerStore = create<ActiveServerState>()(
  persist((set) => ({ index: 0, setIndex: (index) => set({ index }) }), { name: "elsa-studio:active-server" }),
);

/** The backend currently targeted by the UI. Falls back to the first configured server. */
export function useActiveServer(): ServerConfig {
  const config = useStudioConfig();
  const index = useActiveServerStore((s) => s.index);
  return config.servers[index] ?? config.servers[0];
}
