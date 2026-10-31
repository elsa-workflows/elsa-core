import type { ComponentType, ReactNode } from "react";
import type { Activity, ActivityDescriptor } from "@elsa-workflows/react-studio-api-client";
import type { UiHintContext, UiHintEditorProps } from "./editor-types.js";
import type { StudioComponentProps } from "./components.js";
import type {
  DashboardWidget, DefinitionRowAction, InstanceRowAction, InstanceViewerAction,
  ModuleRoute, NavItem, SettingsSection, WorkflowPropertiesTab,
} from "./modules.js";

/** Stable module-local identity. Equal orders are resolved by module ID and slot ID. */
export interface Slot {
  id: string;
  order?: number;
}

/** Designer data and commands available to independently contributed controls. */
export interface DesignerContext {
  root: Activity;
  selected: readonly Activity[];
  readOnly: boolean;
  updateActivity: (id: string, patch: Partial<Activity>) => void;
}

export interface ExpressionEditorProps {
  value: string;
  onChange: (value: string) => void;
  context: UiHintContext;
  readOnly?: boolean;
}

/** SDK v1 Preview contribution contracts; every contribution has an explicit identity and order. */
export interface StudioSlots {
  /** Resources are namespaced to their owning module; English is the fallback. */
  translations: Slot & { culture: string; messages: Readonly<Record<string, string>> };
  components: { [K in keyof StudioComponentProps]: Slot & { name: K; component: ComponentType<StudioComponentProps[K]> } }[keyof StudioComponentProps];
  routes: Slot & ModuleRoute;
  navigation: Slot & NavItem;
  dashboardWidgets: Slot & DashboardWidget;
  settingsSections: Slot & SettingsSection;
  uiHints: Slot & { hint: string; component: ComponentType<UiHintEditorProps> };
  propertyEditors: Slot & { typeName: string; component: ComponentType<UiHintEditorProps> };
  expressionEditors: Slot & { syntax: string; component: ComponentType<ExpressionEditorProps> };
  designerNodes: Slot & { activityType: string; component: ComponentType<{ activity: Activity; selected: boolean }> };
  designerToolbox: Slot & { descriptors: readonly ActivityDescriptor[] };
  designerContainers: Slot & { activityType: string; component: ComponentType<{ activity: Activity; context: DesignerContext }> };
  designerActions: Slot & { label: string; run: (context: DesignerContext) => void | Promise<void> };
  designerPanels: Slot & { placement: "left" | "right" | "bottom"; component: ComponentType<{ context: DesignerContext }> };
  activityPropertiesTabs: Slot & { label: string; component: ComponentType<{ activity: Activity; context: DesignerContext }> };
  workflowPropertiesTabs: Slot & WorkflowPropertiesTab;
  definitionRowActions: Slot & DefinitionRowAction;
  instanceRowActions: Slot & InstanceRowAction;
  instanceViewerActions: Slot & InstanceViewerAction;
  headerItems: Slot & { component: ComponentType };
  providers: Slot & { component: ComponentType<{ children: ReactNode }> };
}

/** Given to a module's exported register function; contributions belong to that module's lifetime. */
export interface StudioApi {
  readonly hostApiVersion: 1;
  readonly sdkApiVersion: 1;
  contribute<K extends keyof StudioSlots>(kind: K, contribution: StudioSlots[K]): () => void;
  /** Register cleanup immediately, including when later registration fails. */
  onDispose(cleanup: () => void): void;
}

export type RegisterModule = (api: StudioApi) => void | (() => void) | Promise<void | (() => void)>;

/** Describes a versioned ESM entry contributed by an enabled server feature. */
export interface StudioModuleManifest {
  id: string;
  version: string;
  hostApiVersion: number;
  sdkApiVersion: number;
  entry: string;
  styles?: readonly string[];
  name?: string;
  description?: string;
  core?: boolean;
  requiresServerFeatures?: string[];
}
