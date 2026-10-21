import { expect, it } from "vitest";
import { serverConfigSchema } from "./config.js";
import { resolveTenant, tenantIdentityKey, tenantSelectionKey } from "./tenancy.js";

it("preserves the legacy fixed tenant header when no policy is supplied", () => {
  expect(resolveTenant({ name: "Legacy", url: "https://example.test/api", tenantId: "fixed" }, { name: "User", tenantId: "claim" }, "selected").tenantId).toBe("fixed");
});

it("uses the configured authenticated claim instead of a header default or stored selection", () => {
  const server = serverConfigSchema.parse({ url: "https://example.test/api", tenantId: "fixed", tenancy: { mode: "claims", claimType: "tenant_id" } });
  expect(resolveTenant(server, { name: "User", claims: { tenant_id: "claimed" } }, "forged").tenantId).toBe("claimed");
  expect(resolveTenant(server, { name: "User", claims: { tenant_id: ["first", "second"] } }).error).toContain("does not specify a tenant");
  expect(resolveTenant(server, { name: "User" }).tenantId).toBeUndefined();
});

it("recognizes Elsa's default claim URI and the empty default-tenant ID", () => {
  const server = serverConfigSchema.parse({ url: "https://example.test/api", tenancy: { mode: "claims" } });
  expect(resolveTenant(server, { name: "User", tenantId: "" }).tenantId).toBe("");
  expect(resolveTenant(server, { name: "User", claims: { "http://schemas.microsoft.com/identity/claims/tenantid": "tenant" } }).tenantId).toBe("tenant");
});

it("accepts only configured selections, including the default tenant", () => {
  const server = serverConfigSchema.parse({ url: "https://example.test/api", tenantId: "a", tenancy: { mode: "selection", tenants: [{ id: "", name: "Default" }, { id: "a", name: "A" }] } });
  expect(resolveTenant(server, undefined, "not-configured").tenantId).toBe("a");
  expect(resolveTenant(server, undefined, "").tenantId).toBe("");
  expect(resolveTenant({ ...server, tenantId: undefined }, undefined).tenantId).toBe("");
});

it("rejects ambiguous or empty tenant choice configuration", () => {
  expect(serverConfigSchema.safeParse({ url: "api", tenancy: { mode: "selection" } }).success).toBe(false);
  expect(serverConfigSchema.safeParse({ url: "api", tenancy: { mode: "selection", tenants: [{ id: "a", name: "A" }, { id: "a", name: "Other" }] } }).success).toBe(false);
});

it("isolates preferences by server and stable user subject", () => {
  const alice = tenantIdentityKey({ name: "Same name", claims: { sub: "alice" } });
  const bob = tenantIdentityKey({ name: "Same name", claims: { sub: "bob" } });
  expect(tenantSelectionKey("https://a.test/api", alice)).not.toBe(tenantSelectionKey("https://a.test/api", bob));
  expect(tenantSelectionKey("https://a.test/api", alice)).not.toBe(tenantSelectionKey("https://b.test/api", alice));
  expect(tenantSelectionKey("https://a.test/api/", alice)).toBe(tenantSelectionKey("https://a.test/api", alice));
});
