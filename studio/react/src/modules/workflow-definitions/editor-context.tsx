"use client";

import { createContext, useContext } from "react";
import type { WorkflowDefinition } from "@/lib/api/types/workflow-definitions";

/**
 * Context available to everything rendered inside the workflow definition editor, including tabs
 * contributed by other modules (`extensions.workflowPropertiesTabs`).
 */
export interface WorkflowEditorContextValue {
  /** The definition currently being edited (root graph excluded; use the designer store for that). */
  definition: WorkflowDefinition;
  /** Patch top-level definition fields (name, description, variables, inputs, outputs, options…). */
  updateDefinition: (patch: Partial<WorkflowDefinition> | ((d: WorkflowDefinition) => Partial<WorkflowDefinition>)) => void;
  readOnly: boolean;
  isDirty: boolean;
  /** Persists the current state; resolves with the saved definition. */
  save: (options?: { publish?: boolean }) => Promise<WorkflowDefinition | undefined>;
}

const WorkflowEditorContext = createContext<WorkflowEditorContextValue | null>(null);

export const WorkflowEditorProvider = WorkflowEditorContext.Provider;

export function useWorkflowEditor(): WorkflowEditorContextValue {
  const ctx = useContext(WorkflowEditorContext);
  if (!ctx) throw new Error("useWorkflowEditor must be used inside the workflow definition editor");
  return ctx;
}
