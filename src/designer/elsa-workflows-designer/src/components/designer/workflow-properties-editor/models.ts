import {TabDefinition, WorkflowDefinition} from "../../../models";

export const WorkflowPropertiesEditorEventTypes = {
  Displaying: 'workflow-properties:displaying'
}

export interface WorkflowPropsUpdatedArgs {
  workflowDefinition: WorkflowDefinition;
}

export interface WorkflowLabelsUpdatedArgs {
  workflowDefinition: WorkflowDefinition;
  labelIds: Array<string>;
}

export interface WorkflowPropertiesEditorDisplayingArgs {
  model: WorkflowPropertiesEditorModel;
}

export interface TabModel {
  name: string;
  tab: TabDefinition;
}

export interface Widget {
  name: string;
  content: () => any;
  order?: number;
}

export interface PropertiesTabModel extends TabModel {
  Widgets: Array<Widget>;
}

export interface WorkflowPropertiesEditorModel {
  tabModels: Array<TabModel>;
  refresh: () => void;
}
