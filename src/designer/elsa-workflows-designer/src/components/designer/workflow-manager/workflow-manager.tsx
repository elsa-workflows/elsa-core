import {Component, h, Method, Prop} from "@stencil/core";
import {ActivityDescriptor, WorkflowDefinition, WorkflowInstance} from "../../../models";

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
    const monacoLibPath = this.monacoLibPath;
    const workflowInstance = this.workflowInstance;
    const workflowDefinition = this.workflowDefinition;
    const activityDescriptors = this.activityDescriptors;

    if (workflowDefinition == null)
      return <div/>;

    if (workflowInstance == null) {
      return <elsa-workflow-definition-editor
        monacoLibPath={monacoLibPath}
        workflowDefinition={workflowDefinition}
        activityDescriptors={activityDescriptors}
        ref={el => this.workflowDefinitionEditor = el}/>
    }

    return <elsa-workflow-instance-viewer
      monacoLibPath={monacoLibPath}
      ref={el => this.workflowInstanceViewer = el}/>
  }
}
