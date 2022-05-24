import {Type} from "./shared";
import {Expression} from "./expressions";

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
  defaultValue?: any;
}

export interface ActivityInput {
  type: Type;
  expression: Expression;
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
  register: RegisterState;
}

export interface RegisterState {
  locations: Map<string, RegisterLocation>;
}

export interface RegisterLocation {
  value: any;
}

export interface Label {
  id: string;
  name: string;
  description?: string;
  color?: string;
}

export enum SyntaxNames {
  Literal = 'Literal',
  JavaScript = 'JavaScript',
  Liquid = 'Liquid',
  Json = 'Json'
}
