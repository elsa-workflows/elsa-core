export interface WorkflowSettings {
  id?: string;
  workflowBlueprintId?: string;
  key?: string;
  value?: string;
  defaultValue?: string;
  description?: string;
}

export interface WorkflowDefinitionProperty extends WorkflowSettings {
}