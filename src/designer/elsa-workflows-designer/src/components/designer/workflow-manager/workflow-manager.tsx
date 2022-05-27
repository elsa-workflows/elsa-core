import {Component, h, Method, Prop} from "@stencil/core";
import {ActivityDescriptor, WorkflowDefinition, WorkflowInstance} from "../../../models";
import {Flowchart} from "../../activities/flowchart/models";

@Component({
  tag: 'elsa-workflow-manager',
  shadow: false
})
export class WorkflowManager {
  private workflowDefinitionEditor?: HTMLElsaWorkflowDefinitionEditorElement;
  private workflowInstanceViewer?: HTMLElsaWorkflowViewerElement;

  @Prop({attribute: 'monaco-lib-path'}) public monacoLibPath: string;
  @Prop() public activityDescriptors: Array<ActivityDescriptor> = [];
  @Prop() public workflowDefinition?: WorkflowDefinition;
  @Prop() public workflowInstance?: WorkflowInstance;

  @Method()
  public async getWorkflowDefinition(): Promise<WorkflowDefinition> {
    if (!this.workflowDefinitionEditor)
      return null;

    return await this.workflowDefinitionEditor.getWorkflowDefinition();
  }

  /**
   * Updates the workflow definition without importing it into the designer.
   */
  @Method()
  public async updateWorkflowDefinition(workflowDefinition: WorkflowDefinition): Promise<void> {
    if (!this.workflowDefinitionEditor)
      return null;

    await this.workflowDefinitionEditor.updateWorkflowDefinition(workflowDefinition);
  }

  public render() {
    const workflowInstance = this.workflowInstance;
    const workflowDefinition = this.workflowDefinition;

    if (workflowDefinition == null)
      return <div/>;

    if (workflowInstance == null) {
      return <elsa-workflow-definition-editor workflowDefinition={workflowDefinition} ref={el => this.workflowDefinitionEditor = el}/>
    }

    return <elsa-workflow-instance-viewer ref={el => this.workflowInstanceViewer = el}/>
  }
}
