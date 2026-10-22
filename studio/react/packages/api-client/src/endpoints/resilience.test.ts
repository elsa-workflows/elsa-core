import { afterEach, expect, it, vi } from "vitest";
import { createElsaClient } from "../client.js";

afterEach(() => vi.unstubAllGlobals());

it("queries retry attempts by encoded execution ID with paging, current auth/tenant, and cancellation", async () => {
  const fetcher = vi.fn(async () => Response.json({ items: [], totalCount: 0 }));
  vi.stubGlobal("fetch", fetcher);
  const client = createElsaClient({ baseUrl: "https://elsa.example.invalid/api", tenantId: "tenant-a", getAuthHeaders: () => ({ Authorization: "Bearer test" }) });
  const controller = new AbortController();
  await client.resilience.retries("execution/a b", { skip: 10, take: 10 }, controller.signal);
  expect(fetcher).toHaveBeenCalledWith("https://elsa.example.invalid/api/resilience/retries/execution%2Fa%20b?skip=10&take=10", expect.objectContaining({
    signal: controller.signal, headers: { Accept: "application/json", Authorization: "Bearer test", "X-Tenant-Id": "tenant-a" },
  }));
  await client.resilience.strategies(controller.signal);
  expect(fetcher).toHaveBeenLastCalledWith("https://elsa.example.invalid/api/resilience/strategies", expect.objectContaining({ signal: controller.signal }));
});
