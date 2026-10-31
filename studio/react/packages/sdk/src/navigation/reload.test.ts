import { afterEach, expect, it, vi } from "vitest";

afterEach(() => { vi.unstubAllGlobals(); vi.resetModules(); });

it("marks a context reload pending before a synchronous store update and navigates once", async () => {
  const assign = vi.fn();
  vi.stubGlobal("document", { baseURI: "https://studio.test/studio-preview/" });
  vi.stubGlobal("window", { location: { assign }, addEventListener: vi.fn(), removeEventListener: vi.fn() });
  const { reloadStudioContext } = await import("./reload.js");
  expect(reloadStudioContext(() => { expect(reloadStudioContext()).toBe(false); })).toBe(true);
  expect(assign).toHaveBeenCalledExactlyOnceWith("/studio-preview/");
  expect(reloadStudioContext()).toBe(false);
});

it("allows retry after a native unsaved-changes guard prevents navigation", async () => {
  const listeners = new EventTarget();
  const assign = vi.fn();
  vi.stubGlobal("document", { baseURI: "https://studio.test/" });
  vi.stubGlobal("window", { location: { assign }, addEventListener: listeners.addEventListener.bind(listeners), removeEventListener: listeners.removeEventListener.bind(listeners) });
  const { reloadStudioContext } = await import("./reload.js");
  reloadStudioContext();
  const event = new Event("beforeunload", { cancelable: true });
  event.preventDefault();
  listeners.dispatchEvent(event);
  await Promise.resolve();
  expect(reloadStudioContext()).toBe(true);
  expect(assign).toHaveBeenCalledTimes(2);
});
