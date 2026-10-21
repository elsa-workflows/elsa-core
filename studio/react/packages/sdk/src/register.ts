import type { StudioModule } from "./modules.js";
import type { StudioApi } from "./slots.js";

/** Register declarative module contributions through the same typed SDK API as external modules. */
export function registerStudioModule(api: StudioApi, module: StudioModule): void {
  // Existing modules declare static routes before parameter routes. Preserve that
  // priority while assigning stable path IDs; new modules can contribute explicit orders.
  module.routes?.forEach((route, order) => api.contribute("routes", { id: route.path, order, ...route }));
  module.nav?.forEach(item => api.contribute("navigation", item));
  const extensions = module.extensions;
  extensions?.components?.forEach(item => api.contribute("components", item));
  extensions?.providers?.forEach(item => api.contribute("providers", item));
  extensions?.dashboardWidgets?.forEach(item => api.contribute("dashboardWidgets", item));
  extensions?.settingsSections?.forEach(item => api.contribute("settingsSections", item));
  extensions?.workflowPropertiesTabs?.forEach(item => api.contribute("workflowPropertiesTabs", item));
  extensions?.definitionRowActions?.forEach(item => api.contribute("definitionRowActions", item));
  extensions?.instanceRowActions?.forEach(item => api.contribute("instanceRowActions", item));
  extensions?.instanceViewerActions?.forEach(item => api.contribute("instanceViewerActions", item));
  extensions?.headerItems?.forEach(item => api.contribute("headerItems", item));
  extensions?.activityTabs?.forEach(item => api.contribute("activityPropertiesTabs", item));
  Object.entries(extensions?.uiHints ?? {}).forEach(([hint, component]) =>
    api.contribute("uiHints", { id: hint, hint, component }));
  if (module.Provider && !extensions?.providers) api.contribute("providers", { id: "provider", component: module.Provider });
}
