import {Component, h, Listen, Prop, State, Method, Watch, Element} from '@stencil/core';
import {Container} from "typedi";
import {PanelPosition, PanelStateChangedArgs} from '../panel/models';
import {
  Activity,
  ActivityDescriptor,
  ActivitySelectedArgs,
  ContainerSelectedArgs,
  GraphUpdatedArgs,
  WorkflowDefinition,
  WorkflowInstance
} from '../../../models';
import WorkflowEditorTunnel, {WorkflowDesignerState} from '../state';
import {PluginRegistry, ActivityNameFormatter, ActivityDriverRegistry, EventBus} from '../../../services';
import {MonacoEditorSettings} from "../../../services/monaco-editor-settings";
import {WorkflowEditorEventTypes} from "../workflow-definition-editor/models";
import descriptorsStore from "../../../data/descriptors-store";

@Component({
  tag: 'elsa-workflow-instance-viewer',
  styleUrl: 'workflow-instance-viewer.scss',
})
export class WorkflowInstanceViewer {
  private readonly pluginRegistry: PluginRegistry;
  private readonly eventBus: EventBus;
  private readonly activityNameFormatter: ActivityNameFormatter;
  private canvas: HTMLElsaCanvasElement;
  private container: HTMLDivElement;

  //private toolbox: HTMLElsaToolboxElement;

  constructor() {
    this.eventBus = Container.get(EventBus);
    this.pluginRegistry = Container.get(PluginRegistry);
    this.activityNameFormatter = Container.get(ActivityNameFormatter);
  }

  @Element() private el: HTMLElsaWorkflowDefinitionEditorElement;
  @Prop({attribute: 'monaco-lib-path'}) public monacoLibPath: string;
  @Prop() public workflowDefinition: WorkflowDefinition;
  @Prop() public workflowInstance: WorkflowInstance;
  @State() private selectedActivity?: Activity;

  @Watch('monacoLibPath')
  private handleMonacoLibPath(value: string) {
    const settings = Container.get(MonacoEditorSettings);
    settings.monacoLibPath = value;
  }

  @Watch('workflowDefinition')
  async onWorkflowDefinitionChanged(value: WorkflowDefinition) {
    await this.importWorkflow(value, this.workflowInstance);
  }

  @Watch('workflowInstance')
  async onWorkflowInstanceChanged(value: WorkflowDefinition) {
    await this.importWorkflow(this.workflowDefinition, this.workflowInstance);
  }

  @Listen('resize', {target: 'window'})
  private async handleResize() {
    await this.updateLayout();
  }

  @Listen('collapsed')
  private async handlePanelCollapsed() {
    this.selectedActivity = null;
  }

  @Listen('containerSelected')
  private async handleContainerSelected(e: CustomEvent<ContainerSelectedArgs>) {
    this.selectedActivity = null;
  }

  @Listen('activitySelected')
  private async handleActivitySelected(e: CustomEvent<ActivitySelectedArgs>) {
    this.selectedActivity = e.detail.activity;
  }

  @Listen('graphUpdated')
  private handleGraphUpdated(e: CustomEvent<GraphUpdatedArgs>) {
  }

  @Method()
  public async getCanvas(): Promise<HTMLElsaCanvasElement> {
    return this.canvas;
  }

  @Method()
  public async registerActivityDrivers(register: (registry: ActivityDriverRegistry) => void): Promise<void> {
    const registry = Container.get(ActivityDriverRegistry);
    register(registry);
  }

  @Method()
  public getWorkflow(): Promise<WorkflowDefinition> {
    return this.getWorkflowInternal();
  }

  @Method()
  public async importWorkflow(workflowDefinition: WorkflowDefinition, workflowInstance: WorkflowInstance): Promise<void> {
    this.workflowInstance = workflowInstance;
    this.workflowDefinition = workflowDefinition;
    await this.canvas.importGraph(workflowDefinition.root);
  }

  // Updates the workflow definition without importing it into the designer.
  @Method()
  public async updateWorkflowDefinition(workflowDefinition: WorkflowDefinition): Promise<void> {
    this.workflowDefinition = workflowDefinition;
  }

  public async componentWillLoad() {
  }

  public async componentDidLoad() {

    if (!!this.workflowDefinition && !!this.workflowInstance)
      await this.importWorkflow(this.workflowDefinition, this.workflowInstance);

    await this.eventBus.emit(WorkflowEditorEventTypes.WorkflowEditor.Ready, this, {workflowEditor: this});
  }

  public render() {
    const workflowInstance = this.workflowInstance;
    const workflowInstanceId = workflowInstance?.id;

    const tunnelState: WorkflowDesignerState = {
      workflow: this.workflowDefinition,
    };

    return (
      <WorkflowEditorTunnel.Provider state={tunnelState}>
        <div class="absolute inset-0" ref={el => this.container = el}>
          <elsa-panel
            class="elsa-activity-picker-container"
            position={PanelPosition.Left}
            onExpandedStateChanged={e => this.onActivityPickerPanelStateChanged(e.detail)}>
            <elsa-workflow-journal workflowInstance={workflowInstance}/>
          </elsa-panel>
          <elsa-canvas
            class="absolute" ref={el => this.canvas = el}
            interactiveMode={false}/>
          <elsa-panel
            class="elsa-activity-editor-container"
            position={PanelPosition.Right}
            onExpandedStateChanged={e => this.onActivityEditorPanelStateChanged(e.detail)}>
            <div class="object-editor-container">
              {this.renderSelectedObject()}
            </div>
          </elsa-panel>
        </div>
      </WorkflowEditorTunnel.Provider>
    );
  }

  private renderSelectedObject = () => {
    const activity = this.selectedActivity;

    if (!!activity)
      return <elsa-activity-properties activity={activity}/>

    return <elsa-workflow-instance-properties workflowDefinition={this.workflowDefinition} workflowInstance={this.workflowInstance}/>;
  }

  private getWorkflowInternal = async (): Promise<WorkflowDefinition> => {
    const root = await this.canvas.exportGraph();
    const workflowDefinition = this.workflowDefinition;
    workflowDefinition.root = root;
    return workflowDefinition;
  };

  private updateLayout = async () => {
    await this.canvas.updateLayout();
  };

  private updateContainerLayout = async (panelClassName: string, panelExpanded: boolean) => {

    if (panelExpanded)
      this.container.classList.remove(panelClassName);
    else
      this.container.classList.toggle(panelClassName, true);

    await this.updateLayout();
  }

  private onActivityPickerPanelStateChanged = async (e: PanelStateChangedArgs) => await this.updateContainerLayout('activity-picker-closed', e.expanded)
  private onActivityEditorPanelStateChanged = async (e: PanelStateChangedArgs) => await this.updateContainerLayout('object-editor-closed', e.expanded)
}
