import {Component, h, Listen, Prop, State, Method, Watch, Element} from '@stencil/core';
import {Container} from "typedi";
import {camelCase} from 'lodash';
import {PanelPosition, PanelStateChangedArgs} from '../panel/models';
import {
  Activity,
  ActivityDescriptor,
  ActivitySelectedArgs,
  ContainerSelectedArgs, EditChildActivityArgs,
  GraphUpdatedArgs,
  WorkflowDefinition,
  WorkflowInstance
} from '../../../models';
import WorkflowEditorTunnel, {WorkflowDesignerState} from '../state';
import {PluginRegistry, ActivityNameFormatter, ActivityDriverRegistry, EventBus, createActivityMap, flatten, walkActivities} from '../../../services';
import {MonacoEditorSettings} from "../../../services/monaco-editor-settings";
import {WorkflowEditorEventTypes} from "../workflow-definition-editor/models";
import descriptorsStore from "../../../data/descriptors-store";
import {WorkflowNavigationItem} from "../workflow-navigator/models";
import {Hash} from "../../../utils";

@Component({
  tag: 'elsa-workflow-instance-viewer',
  styleUrl: 'workflow-instance-viewer.scss',
})
export class WorkflowInstanceViewer {
  private readonly pluginRegistry: PluginRegistry;
  private readonly eventBus: EventBus;
  private readonly activityNameFormatter: ActivityNameFormatter;
  private nodeMap: Hash<Activity> = {};
  private canvas: HTMLElsaCanvasElement;
  private container: HTMLDivElement;

  constructor() {
    this.eventBus = Container.get(EventBus);
    this.pluginRegistry = Container.get(PluginRegistry);
    this.activityNameFormatter = Container.get(ActivityNameFormatter);
  }

  @Element() private el: HTMLElsaWorkflowDefinitionEditorElement;
  @Prop({attribute: 'monaco-lib-path'}) public monacoLibPath: string;
  @Prop() workflowDefinition: WorkflowDefinition;
  @Prop() workflowInstance: WorkflowInstance;
  @State() private workflowDefinitionState: WorkflowDefinition;
  @State() private workflowInstanceState: WorkflowInstance;
  @State() private selectedActivity?: Activity;
  @State() private currentWorkflowPath: Array<WorkflowNavigationItem> = [];

  @Watch('monacoLibPath')
  private handleMonacoLibPath(value: string) {
    const settings = Container.get(MonacoEditorSettings);
    settings.monacoLibPath = value;
  }

  @Watch('workflowDefinition')
  async onWorkflowDefinitionChanged(value: WorkflowDefinition) {
    await this.importWorkflow(value, this.workflowInstanceState);

  }

  @Watch('workflowInstance')
  async onWorkflowInstanceChanged(value: WorkflowDefinition) {
    await this.importWorkflow(this.workflowDefinitionState, this.workflowInstance);
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

  @Listen('editChildActivity')
  private async editChildActivity(e: CustomEvent<EditChildActivityArgs>) {
    const parentActivityId = e.detail.parentActivityId;

    const item: WorkflowNavigationItem = {
      activityId: parentActivityId,
      portName: e.detail.port.name,
    };

    this.currentWorkflowPath = [...this.currentWorkflowPath, item];
    const portName = camelCase(e.detail.port.name);
    const parentActivity = this.nodeMap[parentActivityId];
    const childActivity = parentActivity[portName] as Activity;

    if (!childActivity) {
      await this.canvas.reset();
    } else {
      await this.canvas.importGraph(childActivity);
    }

    this.selectedActivity = null;
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
    this.workflowInstanceState = workflowInstance;
    await this.updateWorkflowDefinition(workflowDefinition);
    await this.canvas.importGraph(workflowDefinition.root);
  }

  // Updates the workflow definition without importing it into the designer.
  @Method()
  public async updateWorkflowDefinition(workflowDefinition: WorkflowDefinition): Promise<void> {
    this.workflowDefinitionState = workflowDefinition;
    this.nodeMap = createActivityMap(flatten(walkActivities(workflowDefinition.root)));

    if (this.currentWorkflowPath.length == 0)
      this.currentWorkflowPath = [{activityId: workflowDefinition.root.id, portName: null}];
  }

  public async componentWillLoad() {
    this.workflowInstanceState = this.workflowInstance;
    await this.updateWorkflowDefinition(this.workflowDefinition);
  }

  public async componentDidLoad() {
    if (!!this.workflowDefinitionState && !!this.workflowInstanceState)
      await this.importWorkflow(this.workflowDefinition, this.workflowInstance);

    await this.eventBus.emit(WorkflowEditorEventTypes.WorkflowEditor.Ready, this, {workflowEditor: this});
  }

  public render() {
    const workflowDefinition = this.workflowDefinitionState;
    const workflowInstance = this.workflowInstanceState;
    const nodeMap = this.nodeMap;

    const tunnelState: WorkflowDesignerState = {
      workflowDefinition: workflowDefinition,
      nodeMap: nodeMap
    };

    return (
      <WorkflowEditorTunnel.Provider state={tunnelState}>
        <div class="absolute inset-0" ref={el => this.container = el}>
          <elsa-workflow-navigator items={this.currentWorkflowPath} workflowDefinition={workflowDefinition} onNavigate={this.onNavigateHierarchy}/>
          <elsa-panel
            class="elsa-activity-picker-container"
            position={PanelPosition.Left}
            onExpandedStateChanged={e => this.onActivityPickerPanelStateChanged(e.detail)}>
            <elsa-workflow-journal workflowDefinition={workflowDefinition} workflowInstance={workflowInstance}/>
          </elsa-panel>
          <elsa-canvas
            class="absolute" ref={el => this.canvas = el}
            interactiveMode={false}/>
          <elsa-panel
            class="elsa-workflow-editor-container"
            position={PanelPosition.Right}
            onExpandedStateChanged={e => this.onActivityEditorPanelStateChanged(e.detail)}>
            <div class="object-editor-container">
              {this.renderSelectedObject()}
            </div>
          </elsa-panel>
          <elsa-panel
            class="elsa-activity-editor-container"
            position={PanelPosition.Bottom}
            onExpandedStateChanged={e => this.onActivityEditorPanelStateChanged(e.detail)}>
            <div class="activity-editor-container">
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

    return <elsa-workflow-instance-properties workflowDefinition={this.workflowDefinitionState} workflowInstance={this.workflowInstanceState}/>;
  }

  private getWorkflowInternal = async (): Promise<WorkflowDefinition> => {
    const root = await this.canvas.exportGraph();
    const workflowDefinition = this.workflowDefinitionState;
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

  private onNavigateHierarchy = async (e: CustomEvent<WorkflowNavigationItem>) => {
    const item = e.detail;
    const activityId = item.activityId;
    let activity = this.nodeMap[activityId];
    const path = this.currentWorkflowPath;
    const index = path.indexOf(item);

    this.currentWorkflowPath = path.slice(0, index + 1);

    if (!!item.portName) {
      const portName = camelCase(item.portName);
      activity = activity[portName] as Activity;
    }

    await this.canvas.importGraph(activity);
  }
}
