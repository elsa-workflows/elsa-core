import {Type} from "./shared";
import {Expression} from "./expressions";

export enum ActivityKind {
  Action = 'Action',
  Trigger = 'Trigger'
}

export type Lookup<T> = { [key: string]: T };

export interface Activity {
  id: string;
  typeName: string;
  metadata: any;
  canStartWorkflow?: boolean;
  applicationProperties: any;

  [name: string]: any;
}

export interface Trigger extends Activity {
}

export interface Container extends Activity {
  activities: Array<Activity>;
  variables: Array<Variable>;
}

export interface Variable {
  name: string;
  type: string;
  value?: any;
  storageDriverId?: string;
}

export interface ActivityInput {
  type: Type;
  expression: Expression;
}

export interface ActivityOutput {
  type: Type;
  memoryReference: MemoryReference;
}

export interface MemoryReference {
  id: string;
}

export interface WorkflowDefinition {
  id: string;
  definitionId: string;
  version: number;
  isLatest: boolean;
  isPublished: boolean;
  name?: string;
  description?: string;
  createdAt?: Date;
  variables?: Array<Variable>;
  metadata?: Map<string, any>;
  applicationProperties?: Map<string, any>;
  materializerName: string;
  materializerContext?: string;
  root: Activity;
}

export interface WorkflowState {
  id: string;
  activityOutput: Map<string, Map<string, any>>;
  completionCallbacks: Array<CompletionCallbackState>;
  activityExecutionContexts: Array<ActivityExecutionContextState>;
  properties: Map<string, any>;
}

export interface ActivityDescriptor {
  activityType: string;
  displayName: string;
  category: string;
  description: string;
  inputs: Array<InputDescriptor>;
  outputs: Array<OutputDescriptor>;
  kind: ActivityKind;
  ports: Array<Port>;
  isContainer: boolean;
}

export interface PropertyDescriptor {
  name: string;
  type: Type;
  displayName?: string;
  description?: string;
  order?: number;
  isBrowsable?: boolean;
}

export interface InputDescriptor extends PropertyDescriptor {
  uiHint?: string;
  options?: any;
  category?: string;
  defaultValue?: any;
  defaultSyntax?: string;
  supportedSyntaxes?: Array<string>;
  isReadOnly?: boolean;
}

export interface OutputDescriptor extends PropertyDescriptor {
}

export interface Port {
  name: string;
  displayName: string;
}

export interface CompletionCallbackState {
  ownerId: string;
  childId: string;
  methodName: string;
}

export interface ActivityExecutionContextState {
  id: string;
  parentActivityExecutionContextId?: string;
  scheduledActivityId: string;
  ownerActivityId?: string;
  properties: Map<string, any>;
}

export interface WorkflowExecutionLogRecord {
  id: string;
  activityInstanceId: string;
  parentActivityInstanceId?: string;
  activityId: string;
  activityType: string;
  timestamp: Date;
  eventName: string;
}

export enum SyntaxNames {
  Literal = 'Literal',
  JavaScript = 'JavaScript',
  Liquid = 'Liquid',
  Json = 'Json'
}

export interface  ActivityMetadata {
  displayText: string;
  designer: ActivityDesignerMetadata;
}

export interface ActivityDesignerMetadata {
  position: Point
}

export interface Point {
  x: number;
  y: number;
}
