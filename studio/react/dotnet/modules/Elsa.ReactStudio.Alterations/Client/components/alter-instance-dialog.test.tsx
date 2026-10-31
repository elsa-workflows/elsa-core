// @vitest-environment jsdom
import { act, type ReactNode } from "react";
import { createRoot } from "react-dom/client";
import { afterEach, expect, it, vi } from "vitest";
import { studioI18n } from "@elsa-workflows/react-studio-sdk/localization";
import { AlterInstanceDialog } from "./alter-instance-dialog";
const { mutateAsync } = vi.hoisted(() => ({ mutateAsync: vi.fn() }));
vi.mock("../hooks", () => { const mutation = () => ({ isPending: false, mutateAsync }); return { useRunAlterations: mutation, useSubmitAlterationPlan: mutation, useRetryWorkflows: mutation }; });
vi.mock("@elsa-workflows/react-studio-ui/common/confirm-dialog", () => ({ useConfirm: () => vi.fn() }));
vi.mock("@elsa-workflows/react-studio-ui/dialog", () => { const Part = ({ children }: { children?: ReactNode }) => <div>{children}</div>; return { Dialog: Part, DialogContent: Part, DialogDescription: Part, DialogFooter: Part, DialogHeader: Part, DialogTitle: Part }; });
vi.mock("@elsa-workflows/react-studio-ui/tabs", () => { const Part = ({ children }: { children?: ReactNode }) => <div>{children}</div>; return { Tabs: Part, TabsContent: Part, TabsList: Part, TabsTrigger: Part }; });
vi.mock("@elsa-workflows/react-studio-ui/common/simple-select", () => ({ SimpleSelect: ({ value, onChange, options }: { value: string; onChange: (value: string) => void; options: { value: string; label: string }[] }) => <select value={value} onChange={event => onChange(event.target.value)}>{options.map(option => <option key={option.value} value={option.value}>{option.label}</option>)}</select> }));
Object.assign(globalThis, { IS_REACT_ACT_ENVIRONMENT: true });
const container = document.body.appendChild(document.createElement("div"));
const root = createRoot(container);
afterEach(async () => { await act(() => root.render(null)); mutateAsync.mockReset(); await studioI18n.changeLanguage("en"); });
it("retranslates rejected alterations without losing their draft values", async () => {
  await act(() => root.render(<AlterInstanceDialog targets={[{ workflowInstanceId: "instance" }]} open onOpenChange={() => {}} />));
  const type = container.querySelector("select")!;
  await act(() => { type.value = "Migrate"; type.dispatchEvent(new Event("change", { bubbles: true })); });
  const version = container.querySelector<HTMLInputElement>('input[type="number"]')!;
  await act(() => { Object.getOwnPropertyDescriptor(HTMLInputElement.prototype, "value")!.set!.call(version, "-1"); version.dispatchEvent(new Event("input", { bubbles: true })); });
  await act(() => [...container.querySelectorAll("button")].find(button => button.textContent === "Submit as plan")!.click());
  expect(container.textContent).toContain("Alteration 1: target version must be a positive integer.");
  await act(async () => { await studioI18n.changeLanguage("nl"); });
  expect(container.textContent).toContain("Wijziging 1: de doelversie moet een positief geheel getal zijn.");
  expect(version.value).toBe("-1"); expect(type.value).toBe("Migrate"); expect(mutateAsync).not.toHaveBeenCalled();
});
