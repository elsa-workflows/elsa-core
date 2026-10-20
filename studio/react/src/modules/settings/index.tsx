import { SettingsRoutes } from "@/lib/modules/settings";
import { defineModule } from "@/lib/modules/types";
import { SettingsPage } from "./pages/settings-page";

export { SettingsRoutes };
export { useSettingsSections } from "./pages/settings-page";

/**
 * Landing page for settings destinations that other modules contribute through
 * `extensions.settingsSections` (plus `settingsSectionNav()` for the sidebar). This module stores no
 * settings itself and adds no sidebar item of its own: like the Blazor studio, "Settings" only
 * appears in the sidebar once at least one module contributes a section.
 */
export const settingsModule = defineModule({
  id: "settings",
  name: "Settings",
  description: "Overview of the configurable capabilities installed in this studio.",
  routes: [{ path: SettingsRoutes.overview, component: SettingsPage, title: "Settings" }],
});
