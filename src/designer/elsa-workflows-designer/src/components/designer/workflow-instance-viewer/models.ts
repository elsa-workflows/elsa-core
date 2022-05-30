import {Activity, ActivityDescriptor, InputDescriptor, TabDefinition, WorkflowDefinition} from "../../../models";

export const WorkflowInstanceViewerEventTypes = {
  WorkflowDefinition: {
    Imported: 'workflow-instance-viewer:workflow-instance:imported'
  },
  WorkflowInstanceViewer: {
    Ready: 'workflow-instance-viewer:ready'
  }
}

export const WorkflowInstancePropertiesEventTypes = {
  Displaying: 'workflow-instance-properties:displaying'
}

export interface WorkflowInstancePropertiesDisplayingArgs {
  model: WorkflowInstancePropertiesViewerModel;
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

export interface WorkflowInstancePropertiesViewerModel {
  tabModels: Array<TabModel>;
}
