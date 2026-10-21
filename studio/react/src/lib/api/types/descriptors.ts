/** Metadata describing available activity types, variable types and strategies. */

export type ActivityKind = "Action" | "Trigger" | "Job" | "Task";
export type PortType = "Embedded" | "Flow";

export interface PortDescriptor {
  name: string;
  displayName?: string;
  type: PortType;
  isBrowsable?: boolean;
}

export interface SelectListItem {
  text: string;
  value: string;
}

export interface InputDescriptor {
  name: string;
  /** CLR-ish type name such as `String`, `Boolean`, `Int32`, `Object`. */
  typeName: string;
  displayName?: string;
  description?: string;
  category?: string;
  uiHint?: string;
  order?: number;
  isWrapped?: boolean;
  isReadOnly?: boolean;
  isSensitive?: boolean;
  storageDriverType?: string;
  evaluatorType?: string;
  isBrowsable?: boolean;
  isSerializable?: boolean;
  isSynthetic?: boolean;
  autoEvaluate?: boolean;
  defaultValue?: unknown;
  defaultSyntax?: string;
  supportedSyntaxes?: string[];
  options?: unknown;
  uiSpecifications?: Record<string, unknown>;
}

export interface OutputDescriptor {
  name: string;
  typeName: string;
  displayName?: string;
  description?: string;
  order?: number;
  isBrowsable?: boolean;
  isSynthetic?: boolean;
}

export interface ActivityDescriptor {
  typeName: string;
  namespace?: string;
  name: string;
  version: number;
  category: string;
  displayName: string;
  description?: string;
  kind: ActivityKind;
  inputs: InputDescriptor[];
  outputs: OutputDescriptor[];
  ports: PortDescriptor[];
  customProperties?: Record<string, unknown>;
  constructionProperties?: Record<string, unknown>;
  isContainer?: boolean;
  isBrowsable?: boolean;
  isStart?: boolean;
  isTerminal?: boolean;
  runAsynchronously?: boolean;
}

export interface VariableDescriptor {
  typeName: string;
  displayName: string;
  category?: string;
  description?: string;
}

export interface StorageDriverDescriptor {
  typeName: string;
  displayName: string;
  priority?: number;
  deprecated?: boolean;
}

export interface ExpressionDescriptor {
  type: string;
  displayName: string;
  isSerializable: boolean;
  isBrowsable: boolean;
  properties: Record<string, unknown>;
}

export interface IncidentStrategyDescriptor {
  typeName: string;
  displayName: string;
  description?: string;
}

export interface WorkflowActivationStrategyDescriptor {
  typeName: string;
  displayName: string;
  description?: string;
}

export interface FeatureDescriptor {
  name: string;
  namespace: string;
  fullName: string;
  displayName: string;
  description?: string;
}
