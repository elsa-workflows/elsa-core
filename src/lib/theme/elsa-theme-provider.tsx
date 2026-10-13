"use client";

import { useEffect, type ReactNode } from "react";
import { useStudioConfig } from "@/config/provider";
import { ELSA_THEME_ATTRIBUTE, useElsaThemeStore, type ElsaThemeId } from "./elsa-theme";

/** Applies the active Elsa theme pack as an attribute on `<html>`; light/dark stays with next-themes. */
export function ElsaThemeProvider({ children }: { children: ReactNode }) {
  const config = useStudioConfig();
  const chosen = useElsaThemeStore((s) => s.theme);
  const active: ElsaThemeId = chosen ?? config.ui.theme;

  useEffect(() => {
    document.documentElement.setAttribute(ELSA_THEME_ATTRIBUTE, active);
  }, [active]);

  return <>{children}</>;
}

export function useElsaTheme() {
  const config = useStudioConfig();
  const chosen = useElsaThemeStore((s) => s.theme);
  const setTheme = useElsaThemeStore((s) => s.setTheme);
  return { theme: (chosen ?? config.ui.theme) as ElsaThemeId, isOverridden: chosen !== null, setTheme, defaultTheme: config.ui.theme };
}
