// @vitest-environment jsdom
import { act, type ReactNode } from "react";
import { createRoot } from "react-dom/client";
import { afterEach, expect, it, vi } from "vitest";
import { studioI18n } from "@elsa-workflows/react-studio-sdk/localization";
import { BpmnImportDialog } from "./bpmn-dialogs";
const { analyze } = vi.hoisted(() => ({ analyze: vi.fn() }));
vi.mock("@elsa-workflows/react-studio-api-client/react", () => ({ useElsaClient: () => ({ bpmn: { analyze } }) }));
vi.mock("@elsa-workflows/react-studio-ui/dialog", () => {
  const Part = ({ children }: { children?: ReactNode }) => <div>{children}</div>;
  return { Dialog: Part, DialogContent: Part, DialogDescription: Part, DialogFooter: Part, DialogHeader: Part, DialogTitle: Part };
});
Object.assign(globalThis, { IS_REACT_ACT_ENVIRONMENT: true });
const container = document.body.appendChild(document.createElement("div"));
const root = createRoot(container);
afterEach(async () => { await act(() => root.render(null)); analyze.mockReset(); await studioI18n.changeLanguage("en"); });
for (const oversized of [false, true]) it(`retranslates ${oversized ? "oversized" : "wrong-extension"} BPMN validation without sending the file`, async () => {
  await act(() => root.render(<BpmnImportDialog onClose={() => {}} onImported={() => {}} />));
  const input = container.querySelector<HTMLInputElement>("#bpmn-file")!;
  const file = new File([oversized ? new Uint8Array(10 * 1024 * 1024 + 1) : "<xml/>"], oversized ? "large.bpmn" : "wrong.xml");
  Object.defineProperty(input, "files", { configurable: true, value: [file] });
  await act(() => input.dispatchEvent(new Event("change", { bubbles: true })));
  expect(container.textContent).toContain("Select a .bpmn document no larger than 10 MB.");
  await act(async () => { await studioI18n.changeLanguage("nl"); });
  expect(container.textContent).toContain("Selecteer een .bpmn-document van maximaal 10 MB.");
  expect(input.files?.[0]).toBe(file); expect(analyze).not.toHaveBeenCalled();
});
it("preserves a server diagnostic through a culture switch", async () => {
  analyze.mockRejectedValue(new Error("Server-owned diagnostic"));
  await act(() => root.render(<BpmnImportDialog onClose={() => {}} onImported={() => {}} />));
  const input = container.querySelector<HTMLInputElement>("#bpmn-file")!;
  Object.defineProperty(input, "files", { value: [new File(["<xml/>"], "valid.bpmn")] });
  await act(async () => { input.dispatchEvent(new Event("change", { bubbles: true })); });
  expect(container.textContent).toContain("Server-owned diagnostic");
  await act(async () => { await studioI18n.changeLanguage("nl"); });
  expect(container.textContent).toContain("Server-owned diagnostic");
});
