import type { StudioModule, StudioModuleExtensions } from "./modules.js";
import { createElement, type ReactNode } from "react";
import type { RegisterModule, StudioApi, StudioModuleManifest, StudioSlots } from "./slots.js";

export interface ModuleDiagnostic {
  moduleId: string;
  code: "incompatible" | "load-failed" | "registration-failed" | "cleanup-failed";
  message: string;
}

type Contribution = { kind: keyof StudioSlots; value: StudioSlots[keyof StudioSlots]; active: boolean };
type RegisteredModule = { manifest: StudioModuleManifest; contributions: Contribution[]; cleanup: (() => void)[] };

/** Host-side SDK runtime. A rejected entry cannot leave partial slots or replace a working module. */
export class StudioRegistry {
  private modules = new Map<string, RegisteredModule>();
  private generations = new Map<string, number>();
  private pending = new Map<string, RegisteredModule>();
  private messages: ModuleDiagnostic[] = [];

  get diagnostics(): readonly ModuleDiagnostic[] {
    return [...this.messages];
  }

  slots<K extends keyof StudioSlots>(kind: K): readonly StudioSlots[K][] {
    return [...this.modules.values()].flatMap(module => module.contributions
      .filter(item => item.active && item.kind === kind)
      .map(item => ({ owner: module.manifest.id, value: item.value as StudioSlots[K] })))
      .sort((a, b) => (a.value.order ?? 0) - (b.value.order ?? 0) ||
        compare(a.owner, b.owner) || compare(a.value.id, b.value.id)).map(item => item.value);
  }

  async load(manifest: StudioModuleManifest, importer: (entry: string) => Promise<{ register: RegisterModule }>): Promise<boolean> {
    const generation = (this.generations.get(manifest.id) ?? 0) + 1;
    this.generations.set(manifest.id, generation);
    const abandoned = this.pending.get(manifest.id);
    this.pending.delete(manifest.id);
    this.messages = this.messages.filter(message => message.moduleId !== manifest.id);
    if (abandoned) this.dispose(abandoned);
    if (manifest.hostApiVersion !== 1 || manifest.sdkApiVersion !== 1) {
      this.messages.push({ moduleId: manifest.id, code: "incompatible",
        message: `Module ${manifest.id} requires host API ${manifest.hostApiVersion} and SDK API ${manifest.sdkApiVersion}; this Preview provides host API 1 and SDK API 1.` });
      return false;
    }

    let entry: { register: RegisterModule };
    try {
      entry = await importer(manifest.entry);
      if (typeof entry.register !== "function") throw new Error("The module must export register(api)");
    } catch (error) {
      if (this.generations.get(manifest.id) === generation) this.messages.push({
        moduleId: manifest.id, code: "load-failed", message: messageOf(error),
      });
      return false;
    }
    if (this.generations.get(manifest.id) !== generation) return false;

    const pending: RegisteredModule = { manifest: { ...manifest }, contributions: [], cleanup: [] };
    this.pending.set(manifest.id, pending);
    let open = true;
    const assertOpen = () => {
      if (!open || this.generations.get(manifest.id) !== generation)
        throw new Error("Contributions must be registered during the active register(api) call");
    };
    const api: StudioApi = {
      hostApiVersion: 1, sdkApiVersion: 1,
      contribute: (kind, value) => {
        assertOpen();
        if (!value.id.trim() || !Number.isFinite(value.order ?? 0)) throw new Error("Slots require a stable ID and finite order");
        if (pending.contributions.some(item => item.kind === kind && item.value.id === value.id && item.active))
          throw new Error(`Duplicate ${kind} slot: ${value.id}`);
        const copy = Object.assign({}, value);
        Object.freeze(copy);
        const contribution: Contribution = { kind, value: copy, active: true };
        pending.contributions.push(contribution);
        return () => { contribution.active = false; };
      },
      onDispose: cleanup => { assertOpen(); pending.cleanup.push(cleanup); },
    };
    try {
      const cleanup = await entry.register(api);
      if (cleanup) pending.cleanup.push(cleanup);
    } catch (error) {
      this.dispose(pending);
      if (this.generations.get(manifest.id) === generation) this.messages.push({
        moduleId: manifest.id, code: "registration-failed", message: messageOf(error),
      });
      return false;
    } finally {
      open = false;
      if (this.pending.get(manifest.id) === pending) this.pending.delete(manifest.id);
    }
    if (this.generations.get(manifest.id) !== generation) {
      this.dispose(pending);
      return false;
    }
    const previous = this.modules.get(manifest.id);
    this.modules.set(manifest.id, pending);
    if (previous) this.dispose(previous);
    return true;
  }

  remove(moduleId: string): void {
    this.generations.set(moduleId, (this.generations.get(moduleId) ?? 0) + 1);
    const module = this.modules.get(moduleId);
    const pending = this.pending.get(moduleId);
    this.modules.delete(moduleId);
    this.pending.delete(moduleId);
    this.messages = this.messages.filter(message => message.moduleId !== moduleId);
    if (module) this.dispose(module);
    if (pending) this.dispose(pending);
  }

  /** Adapts the existing renderer while features migrate to their individual packages. */
  studioModules(): StudioModule[] {
    return [...this.modules.values()].sort((a, b) => compare(a.manifest.id, b.manifest.id)).map(module => {
      const own = <K extends keyof StudioSlots>(kind: K) => this.slots(kind).filter(value =>
        module.contributions.some(item => item.kind === kind && item.active && item.value === value));
      const extensions: StudioModuleExtensions = {
        providers: [...own("providers")],
        dashboardWidgets: [...own("dashboardWidgets")], settingsSections: [...own("settingsSections")],
        workflowPropertiesTabs: own("workflowPropertiesTabs").map(item => ({ ...item, id: JSON.stringify([module.manifest.id, item.id]) })), definitionRowActions: [...own("definitionRowActions")],
        instanceRowActions: [...own("instanceRowActions")], instanceViewerActions: [...own("instanceViewerActions")],
        headerItems: [...own("headerItems")], activityTabs: own("activityPropertiesTabs").map(item => ({ ...item, id: JSON.stringify([module.manifest.id, item.id]) })),
        uiHints: Object.fromEntries(own("uiHints").map(item => [item.hint, item.component])),
      };
      const providers = own("providers");
      return { ...module.manifest, name: module.manifest.name ?? module.manifest.id,
        routes: [...own("routes")], nav: [...own("navigation")], extensions,
        Provider: providers.length ? ({ children }: { children: ReactNode }) =>
          providers.reduceRight((content, provider) => createElement(provider.component, null, content), children) : undefined };
    });
  }

  private dispose(module: RegisteredModule): void {
    for (const cleanup of module.cleanup.splice(0).reverse()) {
      try { cleanup(); }
      catch (error) { this.messages.push({ moduleId: module.manifest.id, code: "cleanup-failed", message: messageOf(error) }); }
    }
  }
}

function compare(a: string, b: string) { return a < b ? -1 : a > b ? 1 : 0; }
function messageOf(error: unknown) { return error instanceof Error ? error.message : "Module activation failed"; }
