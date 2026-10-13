import { Home, Settings } from "lucide-react";
import { NavGroups, type NavItem, type SettingsSection } from "./types";

/** Route patterns owned by the settings module; here so contributing modules don't import the module. */
export const SettingsRoutes = { overview: "/settings" } as const;

/** The shared "Settings" sidebar parent; modules contributing sections merge their children into it by id. */
export const SettingsNavId = "settings";

/**
 * Sidebar entry for a settings section, nested under the shared Settings parent next to "Overview".
 * Put the result in a module's `nav`; the registry merges every module's children into one Settings
 * item, so the item exists only while at least one section is contributed (as in the Blazor studio).
 */
export function settingsSectionNav(section: SettingsSection): NavItem {
  return {
    id: SettingsNavId,
    label: "Settings",
    icon: Settings,
    group: NavGroups.administration.id,
    order: 900,
    children: [
      { id: "settings-overview", label: "Overview", href: SettingsRoutes.overview, icon: Home, order: Number.MIN_SAFE_INTEGER, match: "exact" },
      {
        id: `settings-${section.id}`,
        label: section.label,
        href: section.href,
        icon: section.icon,
        order: section.order,
        permissions: section.permissions,
      },
    ],
  };
}
