import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { authConfigSchema, serverConfigSchema } from "@elsa-workflows/react-studio-sdk/config";
import { createAuthStrategy } from "./create-strategy.js";
import { PermissionSet } from "./permissions.js";
import { ElsaExternalAuthStrategy } from "./strategies/elsa-external.js";
import { readTokens, writeTokens } from "./storage.js";

function storage() {
  const values = new Map<string, string>();
  return {
    getItem: (key: string) => values.get(key) ?? null,
    setItem: (key: string, value: string) => { values.set(key, value); },
    removeItem: (key: string) => { values.delete(key); },
  };
}
const token = (name: string) => `header.${btoa(JSON.stringify({ name, permissions: ["workflows/*:view"], exp: 9_000_000_000 }))}.signature`;
const server = (url: string) => serverConfigSchema.parse({ url });
const identity = authConfigSchema.parse({ provider: "elsa-identity" });

beforeEach(() => {
  vi.stubGlobal("window", { localStorage: storage(), location: { origin: "https://studio.example.invalid", search: "" } });
  vi.stubGlobal("sessionStorage", storage());
  vi.stubGlobal("document", { baseURI: "https://studio.example.invalid/studio-preview/" });
});
afterEach(() => vi.unstubAllGlobals());

describe("shared authentication", () => {
  it("keeps sessions isolated by backend and revokes only the active session on logout", async () => {
    const request = vi.fn(async (url: string) => url.endsWith("/identity/login")
      ? Response.json({ isAuthenticated: true, accessToken: token("Alice"), refreshToken: "refresh-a" })
      : new Response(null, { status: 204 }));
    vi.stubGlobal("fetch", request);
    const a = createAuthStrategy(identity, server("https://a.example.invalid/api"));
    const b = createAuthStrategy(identity, server("https://b.example.invalid/api"));
    await a.login({ username: "Alice", password: "test-only" });
    await b.initialize();
    expect(a.getSnapshot().user?.name).toBe("Alice");
    expect(b.getSnapshot().status).toBe("unauthenticated");
    expect(await b.getAuthHeaders()).toEqual({});
    await a.logout();
    expect(await a.getAuthHeaders()).toEqual({});
    expect(request).toHaveBeenLastCalledWith("https://a.example.invalid/api/identity/logout", expect.objectContaining({
      method: "POST", body: JSON.stringify({ refreshToken: "refresh-a" }),
    }));
  });

  it("shares an in-flight refresh across concurrent API requests", async () => {
    const url = "https://a.example.invalid/api";
    writeTokens(encodeURIComponent(url), { accessToken: "expired", refreshToken: "refresh-a" });
    let finish!: (response: Response) => void;
    const request = vi.fn(() => new Promise<Response>(resolve => { finish = resolve; }));
    vi.stubGlobal("fetch", request);
    const strategy = createAuthStrategy(identity, server(url));
    const first = strategy.tryRefresh();
    const second = strategy.tryRefresh();
    expect(first).toBe(second);
    finish(Response.json({ isAuthenticated: true, accessToken: token("Alice"), refreshToken: "refresh-b" }));
    expect(await first).toBe(true);
    expect(request).toHaveBeenCalledOnce();
    expect(await strategy.getAuthHeaders()).toEqual({ Authorization: `Bearer ${token("Alice")}` });
  });

  it("removes rejected refresh credentials instead of continuing an authenticated session", async () => {
    const url = "https://a.example.invalid/api";
    writeTokens(encodeURIComponent(url), { accessToken: "expired", refreshToken: "rejected" });
    vi.stubGlobal("fetch", vi.fn(async () => new Response(null, { status: 401 })));
    const strategy = createAuthStrategy(identity, server(url));
    expect(await strategy.tryRefresh()).toBe(false);
    expect(strategy.getSnapshot().status).toBe("unauthenticated");
    expect(readTokens(encodeURIComponent(url))).toBeUndefined();
  });

  it.each(["authorization-header", "x-api-key-header"] as const)("retains API key transmission mode %s", async scheme => {
    const strategy = createAuthStrategy(authConfigSchema.parse({ provider: "api-key", apiKey: { key: "test-key", scheme } }), server("https://a.example.invalid/api"));
    await strategy.initialize();
    expect(strategy.getSnapshot().status).toBe("authenticated");
    expect(await strategy.getAuthHeaders()).toEqual(scheme === "authorization-header" ? { Authorization: "ApiKey test-key" } : { "X-Api-Key": "test-key" });
    expect(await strategy.tryRefresh()).toBe(false);
  });

  it("keeps no-auth usable without supplying credentials", async () => {
    const strategy = createAuthStrategy(authConfigSchema.parse({ provider: "none" }), server("https://a.example.invalid/api"));
    await strategy.initialize();
    expect(strategy.getSnapshot()).toEqual({ status: "authenticated", user: { name: "Anonymous" } });
    expect(await strategy.getAuthHeaders()).toEqual({});
    expect(strategy.interactive).toBe(false);
  });

  it.each(["?code=authorization-code", "?code=authorization-code&state=wrong"])("rejects an external callback without matching state: %s", async search => {
    window.location.search = search;
    sessionStorage.setItem("elsa-studio:external-auth:pkce", JSON.stringify({ state: "expected", verifier: "verifier" }));
    const request = vi.fn();
    vi.stubGlobal("fetch", request);
    const strategy = new ElsaExternalAuthStrategy("https://a.example.invalid/api", identity.elsaExternal, "a");
    await expect(strategy.completeSignIn()).rejects.toThrow("Invalid sign-in callback");
    expect(request).not.toHaveBeenCalled();
    expect(sessionStorage.getItem("elsa-studio:external-auth:pkce")).toBeNull();
  });
});

it("preserves unknown versus explicitly empty permissions and resource wildcard boundaries", () => {
  expect(PermissionSet.fromClaims(undefined).has("secrets", "delete")).toBe(true);
  expect(PermissionSet.fromClaims([]).has("secrets", "delete")).toBe(false);
  const grants = PermissionSet.fromClaims(["workflows/*:view"]);
  expect(grants.has("workflows/definitions", "view")).toBe(true);
  expect(grants.has("workflows", "view")).toBe(true);
  expect(grants.has("workflows-other", "view")).toBe(false);
  expect(grants.has("workflows/definitions", "delete")).toBe(false);
});
