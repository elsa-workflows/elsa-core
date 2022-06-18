import {Component, h, Method, Prop, getAssetPath} from "@stencil/core";
import {ActivityDescriptor, WorkflowDefinition, WorkflowInstance} from "../../../models";
import {HomeView} from "./home-view";

@Component({
  tag: 'elsa-workflow-manager',
  styleUrl: 'workflow-manager.css',
  assetsDirs: ['assets'],
  shadow: false
})
export class WorkflowManager {
  private workflowDefinitionEditor?: HTMLElsaWorkflowDefinitionEditorElement;
  private workflowInstanceViewer?: HTMLElsaWorkflowInstanceViewerElement;

  @Prop({attribute: 'monaco-lib-path'}) monacoLibPath: string;
  @Prop() workflowDefinition?: WorkflowDefinition;
  @Prop() workflowInstance?: WorkflowInstance;

  @Method()
  async getWorkflowDefinition(): Promise<WorkflowDefinition> {
    if (!this.workflowDefinitionEditor)
      return null;

    return await this.workflowDefinitionEditor.getWorkflowDefinition();
  }

  /**
   * Updates the workflow definition without importing it into the designer.
   */
  @Method()
  async updateWorkflowDefinition(workflowDefinition: WorkflowDefinition): Promise<void> {
    if (!this.workflowDefinitionEditor)
      return null;

    await this.workflowDefinitionEditor.updateWorkflowDefinition(workflowDefinition);
  }

  @Method()
  async newWorkflow() {
    if (!this.workflowDefinitionEditor)
      return null;

    await this.workflowDefinitionEditor.newWorkflow();
  }

  render() {
    const visualPath = getAssetPath('./assets/elsa-anim.gif');
    const monacoLibPath = this.monacoLibPath;
    const workflowInstance = this.workflowInstance;
    const workflowDefinition = this.workflowDefinition;

    if (workflowDefinition == null) {
      return <HomeView imageUrl={visualPath}/>;
    }

    if (workflowInstance == null) {
      return <elsa-workflow-definition-editor
        monacoLibPath={monacoLibPath}
        workflowDefinition={workflowDefinition}
        ref={el => this.workflowDefinitionEditor = el}/>
    }

    return <elsa-workflow-instance-viewer
      monacoLibPath={monacoLibPath}
      workflowDefinition={workflowDefinition}
      workflowInstance={workflowInstance}
      ref={el => this.workflowInstanceViewer = el}/>
  }
}
