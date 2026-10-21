import type { ActivityInput, Expression } from "@/lib/api/types/activities";
import type { InputDescriptor } from "@/lib/api/types/descriptors";

/** Props every UI-hint editor receives. Editors read/write the wrapped input as a whole. */
export interface UiHintEditorProps {
  descriptor: InputDescriptor;
  value: ActivityInput | undefined;
  onChange: (value: ActivityInput | undefined) => void;
  /** Supported expression types from the server, e.g. Literal, JavaScript, Liquid. */
  syntaxes: string[];
  readOnly?: boolean;
  /** Context the editor may need (workflow definition id for type definitions, activity for option lookups). */
  context: UiHintContext;
}

export interface UiHintContext {
  workflowDefinitionId?: string;
  activityType: string;
  activityVersion: number;
  /** The whole activity object, for editors whose options depend on other inputs. */
  activity: Record<string, unknown>;
  /** Variables declared on the workflow, for the variable/output pickers. */
  variables: { id: string; name: string; typeName: string }[];
  /** Outputs declared by the activity's descriptor. */
  outputs: { name: string; typeName: string; displayName?: string }[];
  /** Workflow inputs, offered by expression autocomplete. */
  inputs?: { name: string }[];
  /** Other activities (name + output names) for `getOutputFrom` completions. */
  activities?: { name: string; outputs: string[] }[];
}

export function literal(value: unknown, typeName = "Object"): ActivityInput {
  return { typeName, expression: { type: "Literal", value } };
}

export function expressionOf(input: ActivityInput | undefined): Expression | undefined {
  return input?.expression;
}
