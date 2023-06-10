import {Type} from "./shared";
import {Expression} from "./expressions";

export enum ActivityKind {
  Action = 'Action',
  Trigger = 'Trigger',
  Job = 'Job',
  Task = 'Task'
}

export type Lookup<T> = { [key: string]: T };

export interface Activity {
  id: string;
  type: string;
  version: number;
  metadata: any;
  canStartWorkflow?: boolean;
  runAsynchronously?: boolean;
  customProperties: any;

  [name: string]: any;
}

export interface Trigger extends Activity {
}

export interface Container extends Activity {
  activities: Array<Activity>;
  variables: Array<Variable>;
}

export interface Workflow extends Activity {
  root: Activity;
  variables: Array<Variable>;
}

export interface Variable {
  id: string;
  name: string;
  typeName: string;
  isArray: boolean;
  value?: any;
  storageDriverTypeName?: string;
}

export interface ActivityInput {
  typeName: Type;
  expression: Expression;
  memoryReference: MemoryReference;
}

export interface ActivityOutput {
  typeName: Type;
  memoryReference: MemoryReference;
}

export interface MemoryReference {
  id: string;
}

export interface VersionedEntity {
  id: string;
  version: number;
  isLatest: boolean;
  isPublished: boolean;
  createdAt?: Date;
}

export interface WorkflowState {
  id: string;
  activityOutput: Map<string, Map<string, any>>;
  completionCallbacks: Array<CompletionCallbackState>;
  activityExecutionContexts: Array<ActivityExecutionContextState>;
  properties: Map<string, any>;
}

export interface ActivityDescriptor {
  typeName: string;
  version: number;
  displayName: string;
  category: string;
  description: string;
  inputs: Array<InputDescriptor>;
  outputs: Array<OutputDescriptor>;
  kind: ActivityKind;
  ports: Array<Port>;
  customProperties: any;
  isContainer: boolean;
  isBrowsable: boolean;
}

export interface PropertyDescriptor {
  name: string;
  typeName: Type;
  displayName?: string;
  description?: string;
  order?: number;
  isBrowsable?: boolean;
}

export interface InputDescriptor extends PropertyDescriptor {
  isWrapped?: boolean;
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
  type: PortType;
  isBrowsable?: boolean;
}

export enum PortType {
  Embedded = 'Embedded',
  Flow = 'Flow'
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
  nodeId: string;
  activityInstanceId: string;
  parentActivityInstanceId?: string;
  activityId: string;
  activityType: string;
  timestamp: Date;
  eventName: string;
  payload?: any;
  activityState: Map<string, any>;
}

export enum SyntaxNames {
  Literal = 'Literal',
  JavaScript = 'JavaScript',
  Liquid = 'Liquid',
  Object = 'Object'
}

export interface ActivityMetadata {
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
