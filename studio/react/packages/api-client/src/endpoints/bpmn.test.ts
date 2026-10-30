import { afterEach, expect, it, vi } from "vitest";
import { createElsaClient } from "../client.js";
import { ApiError } from "../http.js";

afterEach(() => vi.unstubAllGlobals());

it("uploads the original BPMN bytes with process selection and shares auth/tenant headers", async () => {
  const file = new File(["<definitions><!-- unchanged --></definitions>"], "sample.bpmn");
  const fetch = vi.fn(async (_url: string, request: RequestInit) => {
    expect(request.headers).toMatchObject({ Authorization: "Bearer test", "X-Tenant-Id": "tenant" });
    expect(request.headers).not.toHaveProperty("Content-Type");
    const form = request.body as FormData;
    expect(await (form.get("file") as File).text()).toBe(await file.text());
    expect(form.get("ProcessId")).toBe("chosen");
    expect(form.get("DefinitionId")).toBe("existing");
    return Response.json({ id: "version", definitionId: "existing", version: 2, analysis: { processIds: [], issues: [], elementCounts: {} } });
  });
  vi.stubGlobal("fetch", fetch);
  const client = createElsaClient({ baseUrl: "https://example.test/api", tenantId: "tenant", getAuthHeaders: () => ({ Authorization: "Bearer test" }) });
  expect((await client.bpmn.import(file, { processId: "chosen", definitionId: "existing" })).version).toBe(2);
  expect(fetch.mock.calls[0][0]).toBe("https://example.test/api/bpmn/import");
});

it("exports XML unchanged with an encoded definition ID and retains structured refusal data", async () => {
  const xml = '<definitions xmlns="http://www.omg.org/spec/BPMN/20100524/MODEL"/>';
  const refusal = { code: "bpmn.export.source-stale", message: "Changed workflow", data: { elementIds: ["Task1"] } };
  const fetch = vi.fn().mockResolvedValueOnce(new Response(xml, { headers: { "Content-Type": "application/xml" } })).mockResolvedValueOnce(Response.json(refusal, { status: 422 }));
  vi.stubGlobal("fetch", fetch);
  const client = createElsaClient({ baseUrl: "https://example.test/api", getAuthHeaders: () => ({}) });
  expect(await (await client.bpmn.export("definition/one")).text()).toBe(xml);
  expect(fetch.mock.calls[0][0]).toBe("https://example.test/api/bpmn/definitions/definition%2Fone/export");
  try { await client.bpmn.export("definition/one"); expect.fail("Export should refuse stale source"); }
  catch (error) { expect(error).toBeInstanceOf(ApiError); expect((error as ApiError).problem.raw).toEqual(refusal); }
});
