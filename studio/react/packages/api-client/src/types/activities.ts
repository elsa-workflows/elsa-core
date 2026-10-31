/**
 * Activity graph model as stored in a workflow definition's `root` property.
 * Field names mirror the JSON produced by Elsa 3 (camelCase).
 */

export type ExpressionType = "Literal" | "JavaScript" | "CSharp" | "Python" | "Liquid" | "Object" | string;

export interface Expression {
  type: ExpressionType;
  value: unknown;
}

/** A wrapped activity input: `{ typeName, expression, memoryReference }`. */
export interface ActivityInput {
  typeName: string;
  expression?: Expression;
  memoryReference?: { id: string };
}

/** A wrapped activity output: refers to a variable (memory block) by id. */
export interface ActivityOutput {
  typeName: string;
  memoryReference?: { id: string };
}

export interface DesignerMetadata {
  position?: { x: number; y: number };
  size?: { width: number; height: number };
  /** State machine only: canvas position per state name. */
  states?: Record<string, { position?: { x: number; y: number } }>;
  [key: string]: unknown;
}

export interface ActivityMetadata {
  designer?: DesignerMetadata;
  displayText?: string;
  description?: string;
  showDescription?: boolean;
  [key: string]: unknown;
}

export interface ActivityCustomProperties {
  canStartWorkflow?: boolean;
  runAsynchronously?: boolean;
  notFoundConnections?: unknown[];
  [key: string]: unknown;
}

export interface Activity {
  id: string;
  nodeId?: string;
  type: string;
  version: number;
  name?: string;
  metadata?: ActivityMetadata;
  customProperties?: ActivityCustomProperties;
  /** Activity-type-specific properties live next to the well-known ones. */
  [key: string]: unknown;
}

export interface ConnectionEndpoint {
  activity: string;
  port: string;
}

export interface Connection {
  source: ConnectionEndpoint;
  target: ConnectionEndpoint;
  vertices?: { x: number; y: number }[];
}

export interface Variable {
  id: string;
  name: string;
  typeName: string;
  value?: unknown;
  isArray?: boolean;
  storageDriverTypeName?: string;
}

export interface Flowchart extends Activity {
  type: "Elsa.Flowchart";
  activities: Activity[];
  connections: Connection[];
  variables?: Variable[];
  start?: string;
}

/** Ordered container: children run one after another; a terminal child completes the sequence early. */
export interface Sequence extends Activity {
  type: "Elsa.Sequence";
  activities: Activity[];
  variables?: Variable[];
}

/** A named state of a state machine; `entry` / `exit` are optional activities. */
export interface StateMachineState {
  name: string;
  entry?: Activity | null;
  exit?: Activity | null;
}

/** A directed transition. Without a `trigger` it is automatic (evaluated in declaration order). */
export interface StateTransition {
  name?: string | null;
  displayName?: string | null;
  from: string;
  to: string;
  trigger?: Activity | null;
  condition?: ActivityInput | null;
  action?: Activity | null;
}

export interface StateMachine extends Activity {
  type: "Elsa.StateMachine";
  states: StateMachineState[];
  transitions: StateTransition[];
  initialState?: string | null;
  currentState?: string | null;
}

export const FLOWCHART_TYPE = "Elsa.Flowchart";
export const SEQUENCE_TYPE = "Elsa.Sequence";
export const STATE_MACHINE_TYPE = "Elsa.StateMachine";

export function isSequence(a: Activity | undefined | null): a is Sequence {
  return !!a && a.type === SEQUENCE_TYPE && Array.isArray((a as Sequence).activities);
}

export function isStateMachine(a: Activity | undefined | null): a is StateMachine {
  return !!a && a.type === STATE_MACHINE_TYPE && Array.isArray((a as StateMachine).states);
}
export const DEFAULT_IN_PORT = "In";
export const DEFAULT_DONE_PORT = "Done";

export function isFlowchart(a: Activity | undefined | null): a is Flowchart {
  return !!a && a.type === FLOWCHART_TYPE && Array.isArray((a as Flowchart).activities);
}
