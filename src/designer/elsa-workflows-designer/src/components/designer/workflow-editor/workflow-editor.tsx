import {Component, h, Listen, Prop, State, Event, EventEmitter, Method, Watch, Element} from '@stencil/core';
import {debounce} from 'lodash';
import {v4 as uuid} from 'uuid';
import {Container} from "typedi";
import {PanelPosition, PanelStateChangedArgs} from '../panel/models';
import {
  Activity,
  ActivityDescriptor,
  ActivityPropertyChangedEventArgs,
  ActivitySelectedArgs,
  ContainerSelectedArgs,
  EventTypes,
  GraphUpdatedArgs,
  Trigger,
  WorkflowDefinition,
  WorkflowInstance
} from '../../../models';
import WorkflowEditorTunnel, {WorkflowDesignerState} from '../state';
import {
  ActivityUpdatedArgs,
  DeleteActivityRequestedArgs
} from '../activity-properties-editor/activity-properties-editor';
import {ActivityDriverRegistry, EventBus} from '../../../services';
import {WorkflowPropsUpdatedArgs} from "../workflow-properties-editor/workflow-properties-editor";
import {MonacoEditorSettings} from "../../../services/monaco-editor-settings";
import {PluginRegistry} from "../../../services/plugin-registry";
import {Flowchart} from "../../activities/flowchart/models";

export interface WorkflowUpdatedArgs {
  workflow: WorkflowDefinition;
}

@Component({
  tag: 'elsa-workflow-editor',
  styleUrl: 'workflow-editor.scss',
})
export class WorkflowEditor {
  private readonly pluginRegistry: PluginRegistry;
  private readonly eventBus: EventBus;
  private canvas: HTMLElsaCanvasElement;
  private container: HTMLDivElement;
  private toolbox: HTMLElsaToolboxElement;
  private applyActivityChanges: (activity: Activity) => void;
  private deleteActivity: (activity: Activity) => void;
  private applyTriggerChanges: (trigger: Trigger) => void;
  private deleteTrigger: (trigger: Trigger) => void;
  private readonly emitActivityChangedDebounced: (e: ActivityPropertyChangedEventArgs) => void;
  private readonly saveChangesDebounced: () => void;

  @State()
  private workflowDefinition: WorkflowDefinition = {
    root: null,
    id: uuid(),
    definitionId: uuid(),
    version: 1,
    isLatest: true,
    isPublished: false,
    materializerName: 'Json'
  };

  private workflowInstance?: WorkflowInstance;

  constructor() {
    this.eventBus = Container.get(EventBus);
    this.pluginRegistry = Container.get(PluginRegistry);
    this.emitActivityChangedDebounced = debounce(this.emitActivityChanged, 100);
    this.saveChangesDebounced = debounce(this.saveChanges, 1000);
  }

  @Element() el: HTMLElsaWorkflowEditorElement;
  @Prop({attribute: 'monaco-lib-path'}) public monacoLibPath: string;
  @Prop() public activityDescriptors: Array<ActivityDescriptor> = [];
  @Event() public workflowUpdated: EventEmitter<WorkflowUpdatedArgs>
  @State() private selectedActivity?: Activity;

  private get interactiveMode(): boolean {
    return !this.workflowInstance;
  }

  @Watch('monacoLibPath')
  private handleMonacoLibPath(value: string) {
    const settings = Container.get(MonacoEditorSettings);
    settings.monacoLibPath = value;
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
    this.applyActivityChanges = e.detail.applyChanges;
    this.deleteActivity = e.detail.deleteActivity;
  }

  @Listen('graphUpdated')
  private handleGraphUpdated(e: CustomEvent<GraphUpdatedArgs>) {
    if (this.interactiveMode)
      this.saveChangesDebounced();
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
  public async importWorkflow(workflow: WorkflowDefinition, workflowInstance?: WorkflowInstance): Promise<void> {
    this.workflowInstance = workflowInstance;
    await this.importWorkflowMetadata(workflow);
    await this.canvas.importGraph(workflow.root);
  }

  @Method()
  public async importWorkflowMetadata(workflow: WorkflowDefinition): Promise<void> {
    this.workflowDefinition = workflow;
  }

  @Method()
  public async newWorkflow() {
    const flowchart = {
      typeName: 'Elsa.Flowchart',
      activities: [],
      connections: [],
      id: uuid(),
      metadata: {},
      applicationProperties: {},
      variables: []
    } as Flowchart;

    const workflowDefinition: WorkflowDefinition = {
      root: flowchart,
      id: uuid(),
      definitionId: uuid(),
      version: 1,
      isLatest: true,
      isPublished: false,
      materializerName: 'Json'
    }

    await this.importWorkflow(workflowDefinition);
  }

  public render() {
    const tunnelState: WorkflowDesignerState = {
      workflow: this.workflowDefinition,
      activityDescriptors: this.activityDescriptors,
    };

    const interactiveMode = this.interactiveMode;

    return (
      <WorkflowEditorTunnel.Provider state={tunnelState}>
        <div class="absolute inset-0" ref={el => this.container = el}>
          <elsa-panel
            class="elsa-activity-picker-container"
            position={PanelPosition.Left}
            onExpandedStateChanged={e => this.onActivityPickerPanelStateChanged(e.detail)}>
            <elsa-toolbox ref={el => this.toolbox = el}/>
          </elsa-panel>
          <elsa-canvas
            class="absolute" ref={el => this.canvas = el}
            interactiveMode={interactiveMode}
            onDragOver={this.onDragOver}
            onDrop={this.onDrop}/>
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
    if (!!this.selectedActivity)
      return <elsa-activity-properties-editor
        activity={this.selectedActivity}
        onActivityUpdated={e => this.onActivityUpdated(e)}
        onDeleteActivityRequested={e => this.onDeleteActivityRequested(e)}/>

    return <elsa-workflow-properties-editor
      workflow={this.workflowDefinition}
      onWorkflowPropsUpdated={e => this.onWorkflowPropsUpdated(e)}/>;
  }

  private getWorkflowInternal = async (): Promise<WorkflowDefinition> => {
    const root = await this.canvas.exportGraph();
    const workflow = this.workflowDefinition;
    workflow.root = root;
    return workflow;
  };

  private emitActivityChanged = async (activity: Activity, propertyName: string) => {
    await this.eventBus.emit(EventTypes.Activity.PropertyChanged, this, activity, propertyName, this);
  };

  private saveChanges = async (): Promise<void> => {
    const workflow = await this.getWorkflowInternal();
    this.workflowUpdated.emit({workflow});
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

  private onDragOver = (e: DragEvent) => {
    if (this.interactiveMode)
      e.preventDefault();
  };

  private onDrop = async (e: DragEvent) => {
    const json = e.dataTransfer.getData('activity-descriptor');
    const activityDescriptor: ActivityDescriptor = JSON.parse(json);

    await this.canvas.addActivity({descriptor: activityDescriptor, x: e.offsetX, y: e.offsetY});
  };

  private onActivityUpdated = (e: CustomEvent<ActivityUpdatedArgs>) => {
    const updatedActivity = e.detail.activity;
    this.applyActivityChanges(updatedActivity);
    this.emitActivityChangedDebounced({...e.detail, workflowEditor: this.el});
    this.saveChangesDebounced();
  }

  private onWorkflowPropsUpdated = (e: CustomEvent<WorkflowPropsUpdatedArgs>) => this.saveChangesDebounced()

  private onDeleteActivityRequested = (e: CustomEvent<DeleteActivityRequestedArgs>) => {
    this.deleteActivity(e.detail.activity);
    this.selectedActivity = null;
  };
}
