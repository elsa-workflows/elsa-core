// @vitest-environment jsdom
import { act } from "react";
import { createRoot } from "react-dom/client";
import { afterEach, expect, it, vi } from "vitest";
import { studioI18n } from "@elsa-workflows/react-studio-sdk/localization";
import { HttpStatusCodesEditor } from "./http-status-codes-editor";

Object.assign(globalThis, { IS_REACT_ACT_ENVIRONMENT: true });
const container = document.body.appendChild(document.createElement("div"));
const root = createRoot(container);
afterEach(async () => { await act(() => root.render(null)); await studioI18n.changeLanguage("en"); });

it("retranslates a visible status-code validation error without losing the draft", async () => {
  const onChange = vi.fn();
  await act(() => root.render(<HttpStatusCodesEditor descriptor={{ name: "Statuses", typeName: "Int32[]", isWrapped: false }}
    value={undefined} onChange={onChange} syntaxes={["Object"]} readOnly={false}
    context={{ activityType: "Example.Activity", activityVersion: 1, activity: {}, variables: [], outputs: [] }} />));
  const input = container.querySelector("input")!;
  await act(() => {
    Object.getOwnPropertyDescriptor(HTMLInputElement.prototype, "value")!.set!.call(input, "999");
    input.dispatchEvent(new Event("input", { bubbles: true }));
  });
  await act(() => input.dispatchEvent(new KeyboardEvent("keydown", { key: "Enter", bubbles: true })));
  expect(container.querySelector('[role="alert"]')?.textContent).toBe("Enter an HTTP status code from 100 to 599.");
  await act(async () => { await studioI18n.changeLanguage("nl"); });
  expect(container.querySelector('[role="alert"]')?.textContent).toBe("Voer een HTTP-statuscode van 100 tot en met 599 in.");
  expect(input.value).toBe("999");
  expect(onChange).not.toHaveBeenCalled();
});
