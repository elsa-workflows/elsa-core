import { describe, expect, it, vi } from "vitest";
import { StudioRegistry } from "./host.js";
import type { RegisterModule, StudioModuleManifest } from "./slots.js";
import { defineModule } from "./modules.js";
import { createElement } from "react";
import { renderToStaticMarkup } from "react-dom/server";

const manifest = (id: string): StudioModuleManifest => ({
  id, version: "3.11.0", entry: `/_content/${id}/3.11.0/entry.js`, hostApiVersion: 1, sdkApiVersion: 1,
});
const entry = (register: RegisterModule) => async () => ({ register });

describe("SDK module activation", () => {
  it("owns immutable translation snapshots and removes them on unload or failed registration", async () => {
    const registry = new StudioRegistry();
    const messages = { Greeting: "Hallo" };
    expect(await registry.load(manifest("weather"), entry(api => {
      api.contribute("translations", { id: "dutch", culture: "nl-nl", messages });
    }))).toBe(true);
    messages.Greeting = "Changed after registration";
    expect(registry.studioModules()[0].extensions?.translations).toEqual([{ id: "dutch", culture: "nl-NL", messages: { Greeting: "Hallo" } }]);
    expect(await registry.load(manifest("broken"), entry(api => {
      api.contribute("translations", { id: "english", culture: "en", messages: { Greeting: "Partial" } });
      api.contribute("translations", { id: "invalid", culture: "not_a_culture", messages: {} });
    }))).toBe(false);
    expect(registry.slots("translations")).toHaveLength(1);
    registry.remove("weather");
    expect(registry.slots("translations")).toEqual([]);
  });

  it("qualifies host tab identities without changing module-local SDK IDs", async () => {
    const registry = new StudioRegistry();
    for (const id of ["alpha", "beta"]) {
      await registry.load(manifest(id), entry(api => {
        for (const tab of ["details", "properties"]) api.contribute("workflowPropertiesTabs", { id: tab, label: tab, component: () => null });
        api.contribute("activityPropertiesTabs", { id: "details", label: "Details", component: () => null });
      }));
    }
    const modules = registry.studioModules();
    const ids = modules.flatMap(module => module.extensions?.workflowPropertiesTabs?.map(tab => tab.id) ?? []);
    expect(new Set(ids).size).toBe(4);
    expect(ids).not.toContain("properties");
    expect(ids.map(id => JSON.parse(id))).toEqual([["alpha", "details"], ["alpha", "properties"], ["beta", "details"], ["beta", "properties"]]);
    expect(new Set(modules.flatMap(module => module.extensions?.activityTabs?.map(tab => tab.id) ?? [])).size).toBe(2);
    expect(registry.slots("workflowPropertiesTabs").map(tab => tab.id)).toEqual(["details", "properties", "details", "properties"]);
  });

  it("retains cleanup diagnostics when a suspended registration is replaced", async () => {
    const registry = new StudioRegistry();
    let resume!: () => void;
    let started!: () => void;
    const registering = new Promise<void>(resolve => { started = resolve; });
    const paused = new Promise<void>(resolve => { resume = resolve; });
    const first = registry.load(manifest("weather"), entry(async api => {
      api.onDispose(() => { throw new Error("Cleanup failed during replacement"); });
      started();
      await paused;
    }));
    await registering;
    await registry.load(manifest("weather"), entry(() => {}));
    expect(registry.diagnostics).toContainEqual({ moduleId: "weather", code: "cleanup-failed", message: "Cleanup failed during replacement" });
    resume();
    expect(await first).toBe(false);
  });

  it("retains every provider and nests them in contribution order", async () => {
    const registry = new StudioRegistry();
    await registry.load(manifest("providers"), entry(api => {
      api.contribute("providers", { id: "inner", order: 20, component: ({ children }) => createElement("section", { "data-provider": "inner" }, children) });
      api.contribute("providers", { id: "outer", order: 10, component: ({ children }) => createElement("section", { "data-provider": "outer" }, children) });
    }));
    const Provider = registry.studioModules()[0].Provider!;
    expect(renderToStaticMarkup(createElement(Provider, null, "content"))).toBe(
      '<section data-provider="outer"><section data-provider="inner">content</section></section>');
  });
  it("orders slots by explicit order, then module and slot ID regardless of registration order", async () => {
    const registry = new StudioRegistry();
    await registry.load(manifest("zeta"), entry(api => { api.contribute("navigation", { id: "last", label: "last" }); }));
    await registry.load(manifest("alpha"), entry(api => {
      api.contribute("navigation", { id: "z", label: "z" });
      api.contribute("navigation", { id: "a", label: "a" });
      api.contribute("navigation", { id: "priority", label: "first", order: -10 });
    }));
    expect(registry.slots("navigation").map(item => item.id)).toEqual(["priority", "a", "z", "last"]);
  });

  it.each([{ hostApiVersion: 2 }, { sdkApiVersion: 2 }])("rejects incompatible entries before importing them: %j", async incompatible => {
    const registry = new StudioRegistry();
    const importer = vi.fn(entry(() => {}));
    expect(await registry.load({ ...manifest("incompatible"), ...incompatible }, importer)).toBe(false);
    expect(importer).not.toHaveBeenCalled();
    expect(registry.diagnostics[0].code).toBe("incompatible");
  });

  it("rolls back partial registration and runs cleanup without affecting other modules", async () => {
    const registry = new StudioRegistry();
    const cleanup = vi.fn();
    await registry.load(manifest("working"), entry(api => { api.contribute("headerItems", { id: "working", component: () => null }); }));
    expect(await registry.load(manifest("broken"), entry(api => {
      api.onDispose(cleanup);
      api.contribute("headerItems", { id: "partial", component: () => null });
      throw new Error("Registration failed");
    }))).toBe(false);
    expect(registry.slots("headerItems").map(item => item.id)).toEqual(["working"]);
    expect(cleanup).toHaveBeenCalledOnce();
    expect(registry.diagnostics[0].code).toBe("registration-failed");
  });

  it("keeps the working version when its replacement fails", async () => {
    const registry = new StudioRegistry();
    await registry.load(manifest("weather"), entry(api => { api.contribute("navigation", { id: "weather", label: "Working" }); }));
    await registry.load({ ...manifest("weather"), version: "3.11.1" }, entry(() => { throw new Error("Broken update"); }));
    expect(registry.slots("navigation")[0].label).toBe("Working");
  });

  it("removes all slots and runs every cleanup even if one cleanup throws", async () => {
    const registry = new StudioRegistry();
    const order: number[] = [];
    await registry.load(manifest("weather"), entry(api => {
      api.onDispose(() => { order.push(1); });
      api.onDispose(() => { order.push(2); throw new Error("Cleanup failed"); });
      api.contribute("navigation", { id: "weather", label: "Weather" });
      return () => { order.push(3); };
    }));
    registry.remove("weather");
    expect(registry.slots("navigation")).toEqual([]);
    expect(order).toEqual([3, 2, 1]);
    expect(registry.diagnostics[0].code).toBe("cleanup-failed");
  });

  it("does not activate a pending import after the module was removed", async () => {
    const registry = new StudioRegistry();
    let resolve!: (value: { register: RegisterModule }) => void;
    const register = vi.fn();
    const pending = registry.load(manifest("weather"), () => new Promise(done => { resolve = done; }));
    registry.remove("weather");
    resolve({ register });
    expect(await pending).toBe(false);
    expect(register).not.toHaveBeenCalled();
  });

  it("rejects duplicate slot identities and makes removals idempotent", async () => {
    const registry = new StudioRegistry();
    expect(await registry.load(manifest("weather"), entry(api => {
      api.contribute("navigation", { id: "duplicate", label: "first" });
      api.contribute("navigation", { id: "duplicate", label: "second" });
    }))).toBe(false);
    expect(registry.slots("navigation")).toEqual([]);
    registry.remove("weather");
    registry.remove("weather");
    expect(registry.diagnostics).toEqual([]);
  });

  it("cleans up a suspended registration immediately when the module is removed", async () => {
    const registry = new StudioRegistry();
    const cleanup = vi.fn();
    let resume!: () => void;
    let started!: () => void;
    const registering = new Promise<void>(resolve => { started = resolve; });
    const paused = new Promise<void>(resolve => { resume = resolve; });
    const loading = registry.load(manifest("weather"), entry(async api => {
      api.onDispose(cleanup);
      started();
      await paused;
    }));
    await registering;
    registry.remove("weather");
    expect(cleanup).toHaveBeenCalledOnce();
    resume();
    expect(await loading).toBe(false);
    expect(cleanup).toHaveBeenCalledOnce();
  });

  it("adapts existing module routes and extensions through the public API", async () => {
    const registry = new StudioRegistry();
    const module = defineModule({ id: "labels", name: "Labels", routes: [{ path: "/labels", component: () => null }],
      extensions: { workflowPropertiesTabs: [{ id: "labels", label: "Labels", component: () => null }] } });
    await registry.load(manifest(module.id), entry(module.register));
    expect(registry.studioModules()[0].routes?.[0].path).toBe("/labels");
    expect(registry.studioModules()[0].extensions?.workflowPropertiesTabs?.[0].label).toBe("Labels");
  });

  it("preserves declarative static-route priority ahead of parameter routes", async () => {
    const registry = new StudioRegistry();
    const module = defineModule({ id: "secrets", name: "Secrets", routes: [
      { path: "/secrets/new", component: () => null },
      { path: "/secrets/:name", component: () => null },
    ] });
    await registry.load(manifest(module.id), entry(module.register));
    expect(registry.studioModules()[0].routes?.map(route => route.path)).toEqual(["/secrets/new", "/secrets/:name"]);
  });
});
