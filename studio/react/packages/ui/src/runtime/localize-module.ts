import type { NavItem, StudioModule } from "@elsa-workflows/react-studio-sdk";
import type { Translate } from "@elsa-workflows/react-studio-sdk/localization";

/** Only contribution metadata is localized. Workflow names, expressions and backend data stay intact. */
export function localizeModule(module: StudioModule, t: Translate): StudioModule {
  const text = (value: string) => t(value, undefined, module.id);
  const nav = (item: NavItem): NavItem => ({ ...item, label: text(item.label), children: item.children?.map(nav) });
  const labeled = <T extends { label: string }>(item: T): T => ({ ...item, label: text(item.label) });
  const extensions = module.extensions;
  return { ...module, name: text(module.name), description: module.description && text(module.description),
    routes: module.routes?.map(route => ({ ...route, title: route.title && text(route.title) })),
    nav: module.nav?.map(nav),
    extensions: extensions && { ...extensions,
      settingsSections: extensions.settingsSections?.map(section => ({ ...labeled(section), description: section.description && text(section.description) })),
      workflowPropertiesTabs: extensions.workflowPropertiesTabs?.map(labeled),
      definitionRowActions: extensions.definitionRowActions?.map(labeled),
      instanceRowActions: extensions.instanceRowActions?.map(labeled),
      instanceViewerActions: extensions.instanceViewerActions?.map(labeled),
      activityTabs: extensions.activityTabs?.map(labeled),
    } };
}
