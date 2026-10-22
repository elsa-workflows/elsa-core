"use client";
import { message } from "../localization.js";


import { create } from "zustand";
import { persist } from "zustand/middleware";

/** The five Elsa Studio theme packs; colours live in `src/app/globals.css`. */
export const ELSA_THEMES = [
  { id: "classic", label: message("Classic"), description: message("Sky blue on neutral slate"), swatch: "#0EA5E9" },
  { id: "workflow-constellation", label: message("Constellation"), description: message("Deep blue with violet accents"), swatch: "#315EBD" },
  { id: "workflow-aurora", label: message("Aurora"), description: message("Ocean blue with teal accents"), swatch: "#0868BD" },
  { id: "execution-timeline", label: message("Execution Timeline"), description: message("Steel blue, understated"), swatch: "#3977B7" },
  { id: "human-automation", label: message("Human Automation"), description: message("Warm paper with copper accents"), swatch: "#245EA8" },
] as const;

export type ElsaThemeId = (typeof ELSA_THEMES)[number]["id"];

export const ELSA_THEME_ATTRIBUTE = "data-elsa-theme";

interface ElsaThemeState {
  /** `null` means "follow the configured default". */
  theme: ElsaThemeId | null;
  setTheme: (theme: ElsaThemeId | null) => void;
}

export const useElsaThemeStore = create<ElsaThemeState>()(
  persist((set) => ({ theme: null, setTheme: (theme) => set({ theme }) }), { name: "elsa-studio:theme" }),
);

export function isElsaThemeId(value: unknown): value is ElsaThemeId {
  return typeof value === "string" && ELSA_THEMES.some((t) => t.id === value);
}
