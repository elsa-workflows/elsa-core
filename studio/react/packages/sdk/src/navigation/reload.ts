import { studioBasePath } from "../navigation.js";

let pending = false;

/** Reloads all module state once. Preparation runs after the reload is marked
 * pending, so a synchronous server-store update cannot start a second reload. */
export function reloadStudioContext(prepare?: () => void): boolean {
  if (pending) return false;
  pending = true;
  const beforeUnload = (event: BeforeUnloadEvent) => {
    // A native unsaved-changes prompt can cancel navigation. Allow a later retry.
    queueMicrotask(() => { if (event.defaultPrevented) pending = false; });
  };
  window.addEventListener("beforeunload", beforeUnload, { once: true });
  try {
    prepare?.();
    window.location.assign((studioBasePath() || "") + "/");
    return true;
  } catch (error) {
    pending = false;
    window.removeEventListener("beforeunload", beforeUnload);
    throw error;
  }
}
