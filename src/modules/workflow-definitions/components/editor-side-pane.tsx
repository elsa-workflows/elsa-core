"use client";

import { ArrowLeft } from "lucide-react";
import { Button } from "@/components/ui/button";
import { selectionId, StatePanel, TransitionPanel, useDesignerState } from "@/modules/designer";
import { ActivityPropertiesPanel } from "@/modules/activity-editors";
import { useWorkflowEditor } from "../editor-context";
import { WorkflowPropertiesPane } from "./workflow-properties-pane";

/**
 * One right-hand pane: the selected step's properties when something is selected, otherwise the
 * workflow's own settings. Mirrors what n8n and Power Automate do and keeps the canvas full height.
 */
export function EditorSidePane({ onViewVersion }: { onViewVersion: (version: number | undefined) => void }) {
  const { definition } = useWorkflowEditor();
  const selected = useDesignerState((s) => s.selectedActivity());
  const selectedIds = useDesignerState((s) => s.selectedIds);
  const select = useDesignerState((s) => s.select);
  const single = selectedIds.length === 1 ? selectedIds[0] : undefined;

  if (single && (selectionId.isState(single) || selectionId.isTransition(single))) {
    return (
      <div className="flex h-full min-h-0 flex-col">
        <div className="flex items-center gap-1 border-b px-2 py-1">
          <Button variant="ghost" size="xs" onClick={() => select([])} className="text-muted-foreground">
            <ArrowLeft /> Workflow
          </Button>
        </div>
        <div className="min-h-0 flex-1 overflow-y-auto">
          {selectionId.isState(single) ? <StatePanel key={single} stateName={selectionId.stateName(single)} /> : <TransitionPanel key={single} index={selectionId.transitionIndex(single)} />}
        </div>
      </div>
    );
  }

  if (!selected) return <WorkflowPropertiesPane onViewVersion={onViewVersion} />;

  return (
    <div className="flex h-full min-h-0 flex-col">
      <div className="flex items-center gap-1 border-b px-2 py-1">
        <Button variant="ghost" size="xs" onClick={() => select([])} className="text-muted-foreground">
          <ArrowLeft /> Workflow
        </Button>
      </div>
      <div className="min-h-0 flex-1">
        <ActivityPropertiesPanel workflowDefinitionId={definition.definitionId} variables={definition.variables ?? []} workflowOutputs={definition.outputs ?? []} workflowInputs={definition.inputs ?? []} />
      </div>
    </div>
  );
}
