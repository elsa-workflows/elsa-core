"use client";

import { createContext, useContext, type ReactNode } from "react";
import type { StudioConfig } from "./schema";

const StudioConfigContext = createContext<StudioConfig | null>(null);

export function StudioConfigProvider({ config, children }: { config: StudioConfig; children: ReactNode }) {
  return <StudioConfigContext.Provider value={config}>{children}</StudioConfigContext.Provider>;
}

export function useStudioConfig(): StudioConfig {
  const ctx = useContext(StudioConfigContext);
  if (!ctx) throw new Error("useStudioConfig must be used inside <StudioConfigProvider>");
  return ctx;
}

/** Returns the feature flag for a module id. Unknown modules are reported as disabled. */
export function useFeature(id: string) {
  const config = useStudioConfig();
  return config.features[id] ?? { enabled: false, options: {} };
}
