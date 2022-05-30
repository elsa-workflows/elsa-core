import {Activity, ActivityDescriptor, InputDescriptor, TabDefinition, WorkflowDefinition} from "../../../models";

export const WorkflowEditorEventTypes = {
  WorkflowDefinition: {
    Imported: 'workflow-editor:workflow-definition:imported'
  },
  Activity: {
    PropertyChanged: 'workflow-editor:activity:property-changed'
  },
  WorkflowEditor: {
    Ready: 'workflow-editor:ready'
  }
}

export const WorkflowPropertiesEditorEventTypes = {
  Displaying: 'workflow-properties:displaying'
}

export interface ActivityPropertyChangedEventArgs {
  activity: Activity;
  activityDescriptor: ActivityDescriptor;
  propertyName?: string;
  inputDescriptor?: InputDescriptor;
  workflowEditor: HTMLElsaWorkflowDefinitionEditorElement;
}

export interface WorkflowDefinitionUpdatedArgs {
  workflowDefinition: WorkflowDefinition;
}

export interface WorkflowDefinitionPropsUpdatedArgs {
  workflowDefinition: WorkflowDefinition;
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
}

export interface WorkflowDefinitionImportedArgs {
  workflowDefinition: WorkflowDefinition;
}

export interface WorkflowEditorReadyArgs {
  workflowEditor: HTMLElsaWorkflowDefinitionEditorElement;
}
