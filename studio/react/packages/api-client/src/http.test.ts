import { afterEach, expect, it, vi } from "vitest";
import { ApiError, createHttpClient } from "./http.js";

afterEach(() => vi.unstubAllGlobals());

it("distinguishes an explicit default tenant from an unspecified tenant", async () => {
  const request = vi.fn<typeof fetch>(async () => Response.json({}));
  vi.stubGlobal("fetch", request);
  await createHttpClient({ baseUrl: "/api", tenantId: "", getAuthHeaders: () => ({}) }).get("test");
  expect(request.mock.calls[0][1]).toMatchObject({ headers: { "X-Tenant-Id": "" } });
  await createHttpClient({ baseUrl: "/api", getAuthHeaders: () => ({}) }).get("test");
  expect(request.mock.calls[1][1]?.headers).not.toHaveProperty("X-Tenant-Id");
});

it("sends current authentication, tenant, query values and cancellation to the backend", async () => {
  const request = vi.fn(async () => Response.json({ items: [] }));
  vi.stubGlobal("fetch", request);
  const client = createHttpClient({ baseUrl: "https://elsa.example.invalid/api/", tenantId: "tenant-a", getAuthHeaders: () => ({ Authorization: "Bearer test" }) });
  const controller = new AbortController();
  await client.get("workflow-definitions", { query: { page: 0, labels: ["one", "two"], omitted: undefined }, signal: controller.signal });
  expect(request).toHaveBeenCalledWith("https://elsa.example.invalid/api/workflow-definitions?page=0&labels=one&labels=two", expect.objectContaining({
    headers: { Accept: "application/json", Authorization: "Bearer test", "X-Tenant-Id": "tenant-a" }, signal: controller.signal,
  }));
});

it("refreshes once after 401 and obtains fresh headers for the retry", async () => {
  const request = vi.fn().mockResolvedValueOnce(new Response(null, { status: 401 })).mockResolvedValueOnce(Response.json({ ok: true }));
  vi.stubGlobal("fetch", request);
  let token = "expired";
  const client = createHttpClient({ baseUrl: "/api", getAuthHeaders: () => ({ Authorization: token }),
    onUnauthorized: async () => { token = "refreshed"; return true; } });
  expect(await client.get("test")).toEqual({ ok: true });
  expect(request.mock.calls[1][1].headers.Authorization).toBe("refreshed");
});

it("does not loop when a refreshed request is still unauthorized", async () => {
  const request = vi.fn(async () => new Response(null, { status: 401 }));
  const refresh = vi.fn(async () => true);
  vi.stubGlobal("fetch", request);
  const client = createHttpClient({ baseUrl: "/api", getAuthHeaders: () => ({}), onUnauthorized: refresh });
  await expect(client.get("test")).rejects.toBeInstanceOf(ApiError);
  expect(request).toHaveBeenCalledTimes(2);
  expect(refresh).toHaveBeenCalledOnce();
});

it("retains server problem details for caller error handling", async () => {
  vi.stubGlobal("fetch", vi.fn(async () => Response.json({ title: "Validation", detail: "Invalid workflow", errors: { name: ["Required"] } }, { status: 400 })));
  const client = createHttpClient({ baseUrl: "/api", getAuthHeaders: () => ({}) });
  await expect(client.post("workflow-definitions", {})).rejects.toMatchObject({ status: 400, problem: { errors: { name: ["Required"] } } });
});

it("leaves multipart content boundaries to the browser and handles no-content responses", async () => {
  const request = vi.fn(async () => new Response(null, { status: 204 }));
  vi.stubGlobal("fetch", request);
  const client = createHttpClient({ baseUrl: "/api", getAuthHeaders: () => ({}) });
  const form = new FormData();
  form.append("workflow", "test");
  expect(await client.post("import", form)).toBeUndefined();
  expect(request).toHaveBeenCalledWith("/api/import", expect.objectContaining({ body: form, headers: { Accept: "application/json" } }));
});
