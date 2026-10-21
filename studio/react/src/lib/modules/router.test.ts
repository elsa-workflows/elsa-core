import { expect, it } from "vitest";
import { StudioRegistry } from "@elsa-workflows/react-studio-sdk/host";
import type { RegisterModule } from "@elsa-workflows/react-studio-sdk";
import { resolveRoute } from "./router";

it("uses SDK route priorities across modules, independent of activation order", async () => {
  const registry = new StudioRegistry();
  const load = (id: string, register: RegisterModule) => registry.load({
    id, version: "1.0.0", hostApiVersion: 1, sdkApiVersion: 1, entry: id,
  }, async () => ({ register }));
  await load("alpha", api => { api.contribute("routes", { id: "details", order: 10, path: "/details/:id", component: () => null }); });
  await load("zeta", api => { api.contribute("routes", { id: "create", order: -10, path: "/details/new", component: () => null }); });
  expect(resolveRoute(registry.studioModules(), "/details/new")?.module.id).toBe("zeta");
  registry.remove("zeta");
  const match = resolveRoute(registry.studioModules(), "/details/new");
  expect(match?.module.id).toBe("alpha");
  expect(match?.params).toEqual({ id: "new" });
});

it("resolves equal-priority overlapping routes by stable module and slot IDs", () => {
  const a = { id: "alpha", name: "Alpha", routes: [
    { id: "z", path: "/:id", component: () => null },
    { id: "a", path: "/:id", component: () => null },
  ] };
  const z = { id: "zeta", name: "Zeta", routes: [{ path: "/:id", component: () => null }] };
  expect(resolveRoute([z, a], "/example")?.route.id).toBe("a");
  expect(resolveRoute([a, z], "/example")?.route.id).toBe("a");
});
