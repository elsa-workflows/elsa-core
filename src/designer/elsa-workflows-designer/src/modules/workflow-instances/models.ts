import {TabDefinition, WorkflowExecutionLogRecord} from "../../models";
import moment from "moment";

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

export interface ActivityExecutionEventBlock {
  activityId: string;
  activityInstanceId: string;
  parentActivityInstanceId: string;
  completed: boolean;
  faulted: boolean;
  timestamp: Date;
  duration?: moment.Duration;
  startedRecord: WorkflowExecutionLogRecord;
  completedRecord?: WorkflowExecutionLogRecord;
  faultedRecord?: WorkflowExecutionLogRecord;
  children: Array<ActivityExecutionEventBlock>;
}