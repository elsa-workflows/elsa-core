// @vitest-environment jsdom
import { act, type ReactNode } from "react";
import { createRoot } from "react-dom/client";
import { afterEach, expect, it, vi } from "vitest";
import { studioI18n } from "@elsa-workflows/react-studio-sdk/localization";
import type { UserTaskDetail } from "../api";
import { CompleteTaskDialog } from "./complete-task-dialog";

// The modal/select primitives are outside this state-and-validation regression.
vi.mock("@elsa-workflows/react-studio-ui/dialog", () => {
  const Part = ({ children }: { children?: ReactNode }) => <div>{children}</div>;
  return { Dialog: ({ open, children }: { open: boolean; children: ReactNode }) => open ? <div>{children}</div> : null,
    DialogContent: Part, DialogDescription: Part, DialogFooter: Part, DialogHeader: Part, DialogTitle: Part };
});
vi.mock("@elsa-workflows/react-studio-ui/common/simple-select", () => ({
  SimpleSelect: ({ value, onChange, options }: { value: string; onChange: (value: string) => void; options: { value: string; label: string }[] }) =>
    <select value={value} onChange={event => onChange(event.target.value)}>{options.map(option => <option key={option.value} value={option.value}>{option.label}</option>)}</select>,
}));

Object.assign(globalThis, { IS_REACT_ACT_ENVIRONMENT: true });
const container = document.body.appendChild(document.createElement("div"));
const root = createRoot(container);
afterEach(async () => { await act(() => root.render(null)); await studioI18n.changeLanguage("en"); });

for (const form of [true, false]) it(`retranslates ${form ? "required-field" : "JSON"} validation while preserving task data and action`, async () => {
  const actions = [{ key: "approve", label: "Approve" }, { key: "reject", label: "Reject" }];
  const task: UserTaskDetail = { id: "task", title: "Customer task", tags: [], status: "Available", priority: 0, isOverdue: false,
    createdAt: "2026-10-21T00:00:00Z", updatedAt: "2026-10-21T00:00:00Z", allowedActions: ["complete"], revision: 1,
    disclosure: { canViewProtected: false, canViewWorkflow: false, canViewHistory: false, guestVisible: false }, actions,
    form: form ? { provider: "test", key: "customer", actions, fields: [
      { key: "customer", label: "Customer label", type: "text", required: true, masked: false, canReveal: false },
      { key: "note", label: "Server note", type: "text", required: false, masked: false, canReveal: false, value: "Original" },
    ] } : undefined };
  const onSubmit = vi.fn(async () => {});
  await act(() => root.render(<CompleteTaskDialog task={task} open onOpenChange={() => {}} onSubmit={onSubmit} isPending={false} />));
  const select = container.querySelector("select")!;
  await act(() => { select.value = "reject"; select.dispatchEvent(new Event("change", { bubbles: true })); });
  const draft = form ? container.querySelector<HTMLInputElement>("#f-note")! : container.querySelector<HTMLTextAreaElement>("#complete-json")!;
  const value = form ? "Unsaved draft" : "{broken";
  await act(() => {
    const prototype = form ? HTMLInputElement.prototype : HTMLTextAreaElement.prototype;
    Object.getOwnPropertyDescriptor(prototype, "value")!.set!.call(draft, value);
    draft.dispatchEvent(new Event("input", { bubbles: true }));
  });
  await act(() => [...container.querySelectorAll("button")].find(button => button.textContent === "Complete")!.click());
  expect(container.textContent).toContain(form ? "Required: Customer label" : "The payload must be valid JSON.");
  await act(async () => { await studioI18n.changeLanguage("nl"); });
  expect(container.textContent).toContain(form ? "Vereist: Customer label" : "De inhoud moet geldige JSON zijn.");
  expect(container.textContent).toContain("Customer task");
  expect(select.value).toBe("reject");
  expect(draft.value).toBe(value);
  expect(onSubmit).not.toHaveBeenCalled();
});
