import { GitBranch, ListOrdered, Workflow, type LucideIcon } from "lucide-react";
import { FLOWCHART_TYPE, SEQUENCE_TYPE, STATE_MACHINE_TYPE } from "@/lib/api/types/activities";

/**
 * The three container types a workflow — or any branch of one — can be built from.
 *
 * Shared so that choosing a flow reads the same wherever it happens: the new-workflow dialog and the
 * chooser that fills a branch slot (an If's Then, a loop's Body, a Switch case). The descriptions are
 * written for someone deciding, rather than the server's own type documentation.
 */
export interface FlowType {
  type: string;
  label: string;
  description: string;
  icon: LucideIcon;
}

export const FLOW_TYPES: readonly FlowType[] = [
  { type: FLOWCHART_TYPE, label: "Flowchart", description: "Free-form graph of steps joined by outcomes.", icon: Workflow },
  { type: SEQUENCE_TYPE, label: "Sequence", description: "Steps that run one after another.", icon: ListOrdered },
  { type: STATE_MACHINE_TYPE, label: "State machine", description: "Named states with trigger-driven transitions.", icon: GitBranch },
] as const;
