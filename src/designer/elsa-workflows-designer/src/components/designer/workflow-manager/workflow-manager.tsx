import {Component, getAssetPath, h, Method, Prop} from "@stencil/core";
import {ActivityDescriptor, WorkflowDefinition, WorkflowInstance} from "../../../models";
import {Flowchart} from "../../activities/flowchart/models";
import descriptorsStore from "../../../data/descriptors-store";
import {generateUniqueActivityName} from "../../../utils/generate-activity-name";

const FlowchartTypeName = 'Elsa.Flowchart';

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

    const flowchartDescriptor = this.getFlowchartDescriptor();
    const newName = await this.generateUniqueActivityName(flowchartDescriptor);

    const flowchart = {
      typeName: flowchartDescriptor.activityType,
      activities: [],
      connections: [],
      id: newName,
      metadata: {},
      applicationProperties: {},
      variables: []
    } as Flowchart;

    this.workflowDefinition = {
      root: flowchart,
      id: '',
      name: 'Workflow 1',
      definitionId: '',
      version: 1,
      isLatest: true,
      isPublished: false,
      materializerName: 'Json'
    };
  }

  private getFlowchartDescriptor = () => this.getActivityDescriptor(FlowchartTypeName);
  private getActivityDescriptor = (typeName: string): ActivityDescriptor => descriptorsStore.activityDescriptors.find(x => x.activityType == typeName)
  private generateUniqueActivityName = async (activityDescriptor: ActivityDescriptor): Promise<string> => await generateUniqueActivityName([], activityDescriptor);

  render() {
    const monacoLibPath = this.monacoLibPath;
    const workflowInstance = this.workflowInstance;
    const workflowDefinition = this.workflowDefinition;

    if (workflowDefinition == null) {
      return;
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
