import { afterEach, expect, it, vi } from "vitest";
import { createElement as h } from "react";
import { renderToString } from "react-dom/server";
import { AuthProvider, BaseAuthStrategy, type AuthSnapshot } from "@elsa-workflows/react-studio-auth";
import { studioConfigSchema } from "@elsa-workflows/react-studio-sdk/config";
import { StudioConfigProvider } from "@elsa-workflows/react-studio-sdk/config-provider";
import { resolveTenant, TenantProvider } from "@elsa-workflows/react-studio-sdk/tenancy";
import type { ElsaClient } from "@elsa-workflows/react-studio-api-client";
import { ElsaApiProvider, useElsaClient } from "./provider";

const claimType = "http://schemas.microsoft.com/identity/claims/tenantid";
const account = (tenant: string | undefined, subject = "alice"): AuthSnapshot => ({
  status: "authenticated", user: { name: subject, claims: { sub: subject, ...(tenant === undefined ? {} : { [claimType]: tenant }) } },
});

class RefreshingStrategy extends BaseAuthStrategy {
  readonly kind = "elsa-identity" as const;
  readonly interactive = true;
  readonly usesCredentials = true;
  beforeHeaders?: () => Promise<void>;
  beforeRefresh?: () => Promise<void>;
  constructor(snapshot: AuthSnapshot) { super(); this.snapshot = snapshot; }
  replace(snapshot: AuthSnapshot) { this.setSnapshot(snapshot); }
  async initialize() {}
  async login() {}
  async logout() { this.replace({ status: "unauthenticated" }); }
  async getAuthHeaders() {
    await this.beforeHeaders?.();
    return { Authorization: `Bearer ${this.snapshot.user?.claims?.[claimType] ?? "missing"}` };
  }
  async tryRefresh() { await this.beforeRefresh?.(); return this.snapshot.status === "authenticated"; }
}

function captureClient(strategy: RefreshingStrategy): ElsaClient {
  const config = studioConfigSchema.parse({ servers: [{ name: "Test", url: "https://elsa.example.invalid/api", tenancy: { mode: "claims" } }] });
  const tenant = resolveTenant(config.servers[0], strategy.getSnapshot().user);
  let client!: ElsaClient;
  function Capture() { client = useElsaClient(); return null; }
  renderToString(h(StudioConfigProvider, { config, children:
    h(AuthProvider, { strategy, children:
      h(TenantProvider, { value: { ...tenant, selectTenant: () => false }, children:
        h(ElsaApiProvider, { children: h(Capture) }) }) }) }));
  return client;
}

afterEach(() => vi.unstubAllGlobals());

const cases = [
  { name: "same claim", initial: "a", next: account("a"), allowed: true },
  { name: "explicit default claim", initial: "", next: account(""), allowed: true },
  { name: "changed claim", initial: "a", next: account("b"), allowed: false },
  { name: "removed claim", initial: "a", next: account(undefined), allowed: false },
  { name: "changed account with the same tenant", initial: "a", next: account("a", "bob"), allowed: false },
  { name: "logged-out account", initial: "a", next: { status: "unauthenticated" } as AuthSnapshot, allowed: false },
];

for (const phase of ["credentials", "401 refresh"] as const) {
  for (const scenario of cases) {
    it(`${phase}: ${scenario.name} ${scenario.allowed ? "preserves" : "cancels"} the originating workflow save`, async () => {
      const strategy = new RefreshingStrategy(account(scenario.initial));
      const client = captureClient(strategy);
      const change = async () => { await Promise.resolve(); strategy.replace(scenario.next); };
      if (phase === "credentials") strategy.beforeHeaders = change;
      else strategy.beforeRefresh = change;
      const request = vi.fn<typeof fetch>();
      if (phase === "401 refresh") request.mockResolvedValueOnce(new Response(null, { status: 401 }));
      request.mockImplementation(async () => Response.json({ workflowDefinition: { id: "saved" } }));
      vi.stubGlobal("fetch", request);
      const save = client.workflowDefinitions.save({ model: { name: "Origin payload", variables: [], inputs: [], outputs: [], outcomes: [], customProperties: {}, options: {}, root: { id: "root", type: "Elsa.Flowchart", version: 1 } } });
      if (scenario.allowed) {
        await expect(save).resolves.toMatchObject({ workflowDefinition: { id: "saved" } });
        expect(request).toHaveBeenCalledTimes(phase === "credentials" ? 1 : 2);
        for (const [, options] of request.mock.calls) {
          expect(options?.headers).toMatchObject({ "X-Tenant-Id": scenario.initial, Authorization: `Bearer ${scenario.initial}` });
          expect(JSON.parse(options!.body as string)).toMatchObject({ model: { name: "Origin payload" } });
        }
      } else {
        await expect(save).rejects.toThrow("account or tenant changed");
        expect(request).toHaveBeenCalledTimes(phase === "credentials" ? 0 : 1);
      }
    });
  }
}
