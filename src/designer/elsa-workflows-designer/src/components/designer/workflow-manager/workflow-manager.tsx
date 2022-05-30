import {Component, h, Method, Prop, getAssetPath} from "@stencil/core";
import {ActivityDescriptor, WorkflowDefinition, WorkflowInstance} from "../../../models";

@Component({
  tag: 'elsa-workflow-manager',
  styleUrl: 'workflow-manager.css',
  assetsDirs: ['assets'],
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
    const visualPath = getAssetPath('./assets/elsa-anim.gif');
    const monacoLibPath = this.monacoLibPath;
    const workflowInstance = this.workflowInstance;
    const workflowDefinition = this.workflowDefinition;
    const activityDescriptors = this.activityDescriptors;

    if (workflowDefinition == null) {
      return <div class="default-background h-full">
        <div class="flex max-w-5xl mx-auto">
          <div class="flex-grow">
            <div class="ml-10 lg:py-24">
              <h1 class="mt-4 text-4xl tracking-tight font-extrabold sm:mt-5 sm:text-6xl lg:mt-6 xl:text-6xl">
                <span class="block main-title-color">Elsa Workflows</span>
                <span class="pb-3 block sm:pb-5 sub-title-color">3.0</span>
              </h1>
              <p class="text-base text-gray-800 sm:text-xl lg:text-lg xl:text-xl">
                Decoding the future.
              </p>
            </div>
          </div>
          <div class="flex-shrink">
            <img class="" src={visualPath} alt="" width={750}/>
          </div>
        </div>
      </div>;
    }

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
