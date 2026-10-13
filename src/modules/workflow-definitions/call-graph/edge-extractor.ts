import type { Activity, ActivityInput } from "@/lib/api/types/activities";
import { childActivities } from "@/modules/designer/model/containers";
import { CALL_ACTIVITY_TYPES, TARGET_INPUT_NAME, type CallType } from "./config";
import type { CallEdge } from "./types";

/**
 * Finds every Dispatch Workflow / Execute Workflow activity in a definition's activity tree.
 *
 * The walk is iterative (explicit stack) and keeps a visited set of activity ids, so a malformed or
 * self-referencing activity structure terminates. Nested containers of every kind are entered through
 * the designer's `childActivities`, which understands Flowchart, Sequence, StateMachine and embedded
 * ports (If, ForEach, …) without needing activity descriptors.
 *
 * The target is read from the `workflowDefinitionId` input. Only a Literal expression can be resolved
 * statically; any other expression (JavaScript, Liquid, …) yields an edge with `targetId: undefined`
 * so the caller can render an "unresolved target". Such calls can never be matched on the incoming
 * side of another workflow: nothing short of evaluating the expression could tell where they go.
 */
export function extractCallEdges(callerId: string, root: Activity | undefined | null): CallEdge[] {
  const edges: CallEdge[] = [];
  if (!root) return edges;
  const visited = new Set<string>();
  const stack: Activity[] = [root];
  while (stack.length > 0) {
    const activity = stack.pop()!;
    if (!activity || typeof activity !== "object") continue;
    const key = activity.id || `${activity.type}@${stack.length}`;
    if (visited.has(key)) continue;
    visited.add(key);

    const callType = CALL_ACTIVITY_TYPES[activity.type];
    if (callType) {
      const target = readTarget(activity[TARGET_INPUT_NAME] as ActivityInput | string | undefined);
      edges.push({
        callerId,
        targetId: target.literal,
        callType,
        activityId: activity.id,
        activityType: activity.type,
        expressionType: target.expressionType,
      });
    }

    // Push in reverse so siblings pop in document order (edges keep the order of the tree).
    const children = safeChildren(activity);
    for (let i = children.length - 1; i >= 0; i--) if (!visited.has(children[i].id)) stack.push(children[i]);
  }
  return edges;
}

/** The call an activity makes, if it is a Dispatch/Execute Workflow step: literal target id or an unresolved expression. */
export function readCallTarget(activity: Activity): { callType: CallType; targetId?: string; expressionType?: string } | undefined {
  const callType = CALL_ACTIVITY_TYPES[activity.type];
  if (!callType) return undefined;
  const target = readTarget(activity[TARGET_INPUT_NAME] as ActivityInput | string | undefined);
  return { callType, targetId: target.literal, expressionType: target.expressionType };
}

function safeChildren(activity: Activity): Activity[] {
  try {
    return childActivities(activity);
  } catch {
    return [];
  }
}

function readTarget(input: ActivityInput | string | undefined): { literal?: string; expressionType?: string } {
  if (!input) return {};
  // Tolerate a bare string (older serialisations) as a literal.
  if (typeof input === "string") return { literal: input.trim() || undefined };
  const expression = input.expression;
  if (!expression) return {};
  const type = String(expression.type ?? "");
  if (type === "Literal" || type === "") {
    const value = expression.value;
    if (typeof value === "string") return { literal: value.trim() || undefined };
    return {};
  }
  return { expressionType: type };
}
