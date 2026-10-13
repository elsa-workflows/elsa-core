import type { ComponentType, ReactNode } from "react";
import type { LucideIcon } from "lucide-react";
import type { Permission } from "@/lib/permissions";

/** Parameters extracted from a route pattern such as `/workflows/definitions/:definitionId/edit`. */
export type RouteParams = Record<string, string>;

export interface ModuleRoute {
  /** Path pattern. Segments starting with `:` are parameters; a trailing `*` matches the rest. */
  path: string;
  component: ComponentType<{ params: RouteParams }>;
  /** Page title for the browser tab / header. */
  title?: string;
  /** All listed permissions are required to open the route. */
  permissions?: Permission[];
}

export interface NavItem {
  id: string;
  label: string;
  href?: string;
  icon?: LucideIcon;
  /** Group id; see `NavGroups`. */
  group?: string;
  order?: number;
  /** `exact` matches only the href; `prefix` (default) also matches sub-routes. */
  match?: "exact" | "prefix";
  /** The item is shown when the user holds at least one of these. */
  permissions?: Permission[];
  /** Optional live badge rendered at the end of the item (e.g. a running-instances count). */
  badge?: ComponentType;
  children?: NavItem[];
}

export type DashboardZone = "metrics" | "trend" | "primary" | "activity" | "findings" | "secondary" | "status";

export interface DashboardWidget {
  id: string;
  zone: DashboardZone;
  order?: number;
  component: ComponentType<{ range: DashboardRange }>;
  permissions?: Permission[];
}

export type DashboardRange = "1h" | "24h" | "7d";

/** A tab contributed to the workflow-definition properties pane. */
export interface WorkflowPropertiesTab {
  id: string;
  label: string;
  order?: number;
  component: ComponentType;
}

/** Helpers handed to list actions so modules can refresh lists or open dialogs without owning them. */
export interface ListActionHelpers {
  refresh: () => void;
  navigate: (href: string) => void;
}

/** Row action contributed to the workflow definitions list. */
export interface DefinitionRowAction {
  id: string;
  label: string;
  icon?: LucideIcon;
  permissions?: Permission[];
  isVisible?: (definition: import("@/lib/api/types/workflow-definitions").WorkflowDefinitionSummary) => boolean;
  onSelect: (definition: import("@/lib/api/types/workflow-definitions").WorkflowDefinitionSummary, helpers: ListActionHelpers) => void | Promise<void>;
}

/** Row action contributed to the workflow instances list. */
export interface InstanceRowAction {
  id: string;
  label: string;
  icon?: LucideIcon;
  permissions?: Permission[];
  isVisible?: (instance: import("@/lib/api/types/workflow-instances").WorkflowInstanceSummary) => boolean;
  onSelect: (instance: import("@/lib/api/types/workflow-instances").WorkflowInstanceSummary, helpers: ListActionHelpers) => void | Promise<void>;
}

/** Toolbar action rendered in the workflow instance viewer header. */
export interface InstanceViewerAction {
  id: string;
  label: string;
  icon?: LucideIcon;
  permissions?: Permission[];
  isVisible?: (instance: import("@/lib/api/types/workflow-instances").WorkflowInstance) => boolean;
  onSelect: (instance: import("@/lib/api/types/workflow-instances").WorkflowInstance, helpers: ListActionHelpers) => void | Promise<void>;
}

/**
 * A settings destination contributed by a module (mirrors the Blazor `SettingsSectionDescriptor`).
 * The settings module lists these as cards on `/settings`; use `settingsSectionNav()` to add the
 * matching sidebar entry. The section owns its page, data and permissions; nothing is stored here.
 */
export interface SettingsSection {
  /** Unique across modules (case-insensitive). */
  id: string;
  label: string;
  description?: string;
  href: string;
  icon?: LucideIcon;
  order?: number;
  /** Shown when the user holds at least one of these. */
  permissions?: Permission[];
}

export interface StudioModuleExtensions {
  settingsSections?: SettingsSection[];
  dashboardWidgets?: DashboardWidget[];
  /** Tabs added to the workflow-definition properties pane (use `useWorkflowEditor()` for context). */
  workflowPropertiesTabs?: WorkflowPropertiesTab[];
  definitionRowActions?: DefinitionRowAction[];
  instanceRowActions?: InstanceRowAction[];
  instanceViewerActions?: InstanceViewerAction[];
  /** Extra tabs in the activity properties panel. */
  activityTabs?: { id: string; label: string; order?: number; component: ComponentType }[];
  /** Extra components rendered in the header bar (environment picker, language picker ...). */
  headerItems?: { id: string; order?: number; component: ComponentType }[];
  /** Extra UI-hint editors keyed by hint name (e.g. `secret-picker`). */
  uiHints?: Record<string, ComponentType<import("@/modules/activity-editors/types").UiHintEditorProps>>;
}

export interface StudioModule {
  /** Feature-flag key, also used as the stable identity of the module. */
  id: string;
  name: string;
  description?: string;
  /** Core modules are always enabled and ignore configuration. */
  core?: boolean;
  /** Server features that must be installed for this module to show (see `/features/installed`). */
  requiresServerFeatures?: string[];
  routes?: ModuleRoute[];
  nav?: NavItem[];
  extensions?: StudioModuleExtensions;
  /** Optional wrapper around the whole app (context providers a module needs everywhere). */
  Provider?: ComponentType<{ children: ReactNode }>;
}

export const NavGroups = {
  general: { id: "general", label: "General", order: 0 },
  diagnostics: { id: "diagnostics", label: "Diagnostics", order: 100 },
  administration: { id: "administration", label: "Administration", order: 1000 },
} as const;

export function defineModule(module: StudioModule): StudioModule {
  return module;
}
