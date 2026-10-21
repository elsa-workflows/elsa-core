import { Palette } from "lucide-react";
import { settingsSectionNav } from "@elsa-workflows/react-studio-ui/runtime/settings";
import { defineModule, type SettingsSection } from "@elsa-workflows/react-studio-sdk";
import { AppearancePage } from "./pages/appearance-page";
import { AppearanceRoutes } from "./routes";

export { AppearanceRoutes };

const section: SettingsSection = {
  id: "appearance",
  label: "Appearance",
  description: "Light or dark mode and the Elsa colour palette used by this browser.",
  href: AppearanceRoutes.settings,
  icon: Palette,
  order: 10,
};

/**
 * Settings section for the per-browser appearance choices (the same state the header palette menu
 * edits). Also the reference implementation of a `settingsSections` contribution.
 */
export const appearanceModule = defineModule({
  id: "appearance",
  name: "Appearance",
  description: "Appearance settings page (theme pack and light/dark mode).",
  routes: [{ path: AppearanceRoutes.settings, component: AppearancePage, title: "Appearance" }],
  nav: [settingsSectionNav(section)],
  extensions: { settingsSections: [section] },
});
