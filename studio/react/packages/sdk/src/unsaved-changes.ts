import { useEffect } from "react";

const dirtyEditors = new Set<symbol>();
let navigationApproved = false;
const approvedEvents = new WeakSet<Event>();

/** Registers actual unsaved state so a host context switch can offer cancellation. */
export function useUnsavedChanges(dirty: boolean): void {
  useEffect(() => {
    if (!dirty) return;
    const editor = Symbol();
    dirtyEditors.add(editor);
    const beforeUnload = (event: BeforeUnloadEvent) => {
      if (navigationApproved) { navigationApproved = false; approvedEvents.add(event); }
      if (!approvedEvents.has(event)) event.preventDefault();
    };
    window.addEventListener("beforeunload", beforeUnload);
    return () => { dirtyEditors.delete(editor); window.removeEventListener("beforeunload", beforeUnload); };
  }, [dirty]);
}

export function hasUnsavedChanges(): boolean { return dirtyEditors.size > 0; }

/** After explicit discard confirmation, allow the next browser navigation
 * without asking a second time. A later navigation receives its own guard. */
export function approveNavigation(): void { navigationApproved = true; }
