// @vitest-environment jsdom
import { act } from "react";
import { createRoot } from "react-dom/client";
import { afterEach, expect, it } from "vitest";
import { studioI18n } from "@elsa-workflows/react-studio-sdk/localization";
import { CallGraphService } from "../../call-graph/call-graph-service";
import { DEFAULT_CALL_GRAPH_CONFIG } from "../../call-graph/config";
import { definition, fakeApi, flowchart } from "../../call-graph/__tests__/helpers";
import { CallContextStatus } from "./call-context-status";
import type { CallContextState } from "./use-call-context";

Object.assign(globalThis, { IS_REACT_ACT_ENVIRONMENT: true });
const container = document.createElement("div");
const root = createRoot(container);
afterEach(async () => { await act(() => root.render(null)); await studioI18n.changeLanguage("en"); });

it("updates a cached graph's warning tooltip and counts on a live culture change without refetching", async () => {
  const { api, calls } = fakeApi(Array.from({ length: 6 }, (_, i) => definition(`d${i}`, flowchart([]))));
  const service = new CallGraphService(api, { ...DEFAULT_CALL_GRAPH_CONFIG, listPageSize: 2, maxRequests: 1 }, { warn: () => {} });
  const graph = await service.build();
  const state = { status: graph.status, warnings: graph.warnings, counts: { incoming: 1, outgoing: 2 }, truncated: { incoming: false, outgoing: false }, refresh: () => {} } as CallContextState;
  await act(() => root.render(<CallContextStatus state={state} />));
  expect(container.querySelector('[title^="Request limit"]')?.getAttribute("title")).toBe("Request limit of 1 reached; showing what was loaded.");
  expect(container.textContent).toContain("1 caller · 2 callees");
  await act(async () => { await studioI18n.changeLanguage("nl"); });
  expect(container.querySelector('[title^="Verzoeklimiet"]')?.getAttribute("title")).toBe("Verzoeklimiet van 1 bereikt; alleen geladen gegevens worden getoond.");
  expect(container.textContent).toContain("1 aanroeper · 2 aangeroepen werkstromen");
  expect(calls.list).toBe(1);
  expect(service.graph.warnings).toBe(graph.warnings);
});
