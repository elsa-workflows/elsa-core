"use client";

import { create } from "zustand";

/**
 * Row/viewer actions are plain callbacks (not components), so they cannot render a dialog
 * themselves. They push a request into this store and the module `Provider` hosts the dialogs.
 */
export interface AlterTarget {
  workflowInstanceId: string;
  definitionId?: string;
  version?: number;
  name?: string;
  /** Activity ids currently faulted (from incidents), used to pre-fill the retry tab. */
  faultedActivityIds?: string[];
}

interface AlterationDialogState {
  alter?: { targets: AlterTarget[]; onDone?: () => void };
  retry?: { workflowInstanceIds: string[]; onDone?: () => void };
  openAlter: (targets: AlterTarget[], onDone?: () => void) => void;
  openRetry: (workflowInstanceIds?: string[], onDone?: () => void) => void;
  close: () => void;
}

export const useAlterationDialogs = create<AlterationDialogState>((set) => ({
  openAlter: (targets, onDone) => set({ alter: { targets, onDone }, retry: undefined }),
  openRetry: (workflowInstanceIds = [], onDone) => set({ retry: { workflowInstanceIds, onDone }, alter: undefined }),
  close: () => set({ alter: undefined, retry: undefined }),
}));
