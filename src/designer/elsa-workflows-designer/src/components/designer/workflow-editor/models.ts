import {WorkflowDefinition} from "../../../models";

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

export interface WorkflowDefinitionImportedArgs {
  workflowDefinition: WorkflowDefinition;
}

export interface WorkflowEditorReadyArgs {
  workflowEditor: HTMLElsaWorkflowEditorElement;
}
