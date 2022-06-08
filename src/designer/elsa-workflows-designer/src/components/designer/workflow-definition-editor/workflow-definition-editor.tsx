import {Component, h, Listen, Prop, State, Event, EventEmitter, Method, Watch, Element} from '@stencil/core';
import {debounce} from 'lodash';
import {v4 as uuid} from 'uuid';
import {Container} from "typedi";
import {PanelPosition, PanelStateChangedArgs} from '../panel/models';
import {
  Activity,
  ActivityDescriptor,
  ActivitySelectedArgs,
  ContainerSelectedArgs,
  GraphUpdatedArgs,
  WorkflowDefinition
} from '../../../models';
import WorkflowEditorTunnel, {WorkflowDesignerState} from '../state';
import {
  ActivityUpdatedArgs,
  DeleteActivityRequestedArgs
} from './activity-properties-editor';
import {PluginRegistry, ActivityNameFormatter, ActivityDriverRegistry, EventBus} from '../../../services';
import {MonacoEditorSettings} from "../../../services/monaco-editor-settings";
import {Flowchart} from "../../activities/flowchart/models";
import {flattenList, walkActivities} from "../../activities/flowchart/activity-walker";
import {ActivityPropertyChangedEventArgs, WorkflowDefinitionPropsUpdatedArgs, WorkflowDefinitionUpdatedArgs, WorkflowEditorEventTypes} from "./models";

@Component({
  tag: 'elsa-workflow-definition-editor',
  styleUrl: 'workflow-definition-editor.scss',
})
export class WorkflowDefinitionEditor {
  @Element() el: HTMLElsaWorkflowDefinitionEditorElement;

  private readonly pluginRegistry: PluginRegistry;
  private readonly eventBus: EventBus;
  private readonly activityNameFormatter: ActivityNameFormatter;
  private canvas: HTMLElsaCanvasElement;
  private container: HTMLDivElement;
  private toolbox: HTMLElsaWorkflowDefinitionEditorToolboxElement;
  private applyActivityChanges: (activity: Activity) => void;
  private deleteActivity: (activity: Activity) => void;
  private readonly emitActivityChangedDebounced: (e: ActivityPropertyChangedEventArgs) => void;
  private readonly saveChangesDebounced: () => void;

  constructor() {
    this.eventBus = Container.get(EventBus);
    this.pluginRegistry = Container.get(PluginRegistry);
    this.activityNameFormatter = Container.get(ActivityNameFormatter);
    this.emitActivityChangedDebounced = debounce(this.emitActivityChanged, 100);
    this.saveChangesDebounced = debounce(this.saveChanges, 1000);
  }

  @Prop() workflowDefinition?: WorkflowDefinition;
  @Prop({attribute: 'monaco-lib-path'}) public monacoLibPath: string;
  @Prop() public activityDescriptors: Array<ActivityDescriptor> = [];
  @Event() public workflowUpdated: EventEmitter<WorkflowDefinitionUpdatedArgs>
  @State() private workflowDefinitionState: WorkflowDefinition;
  @State() private selectedActivity?: Activity;

  @Watch('monacoLibPath')
  private handleMonacoLibPath(value: string) {
    const settings = Container.get(MonacoEditorSettings);
    settings.monacoLibPath = value;
  }

  @Watch('workflowDefinition')
  async onWorkflowDefinitionChanged(value: WorkflowDefinition) {
    await this.importWorkflow(value);
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
  public getWorkflowDefinition(): Promise<WorkflowDefinition> {
    return this.getWorkflowDefinitionInternal();
  }

  @Method()
  public async importWorkflow(workflowDefinition: WorkflowDefinition): Promise<void> {
    this.workflowDefinitionState = workflowDefinition;
    await this.canvas.importGraph(workflowDefinition.root);
    await this.eventBus.emit(WorkflowEditorEventTypes.WorkflowDefinition.Imported, this, {workflowDefinition});
  }

  // Updates the workflow definition without importing it into the designer.
  @Method()
  public async updateWorkflowDefinition(workflowDefinition: WorkflowDefinition): Promise<void> {
    this.workflowDefinitionState = workflowDefinition;
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
      id: '',
      definitionId: '',
      version: 1,
      isLatest: true,
      isPublished: false,
      materializerName: 'Json'
    }

    await this.importWorkflow(workflowDefinition);
  }

  public async componentWillLoad() {
    this.workflowDefinitionState = this.workflowDefinition;
  }

  public async componentDidLoad() {

    if (!this.workflowDefinitionState)
      await this.newWorkflow();
    else
      await this.importWorkflow(this.workflowDefinitionState);

    await this.eventBus.emit(WorkflowEditorEventTypes.WorkflowEditor.Ready, this, {workflowEditor: this});
  }

  public render() {
    const tunnelState: WorkflowDesignerState = {
      workflow: this.workflowDefinitionState,
      activityDescriptors: this.activityDescriptors,
    };

    return (
      <WorkflowEditorTunnel.Provider state={tunnelState}>
        <div class="absolute inset-0" ref={el => this.container = el}>
          <elsa-panel
            class="elsa-activity-picker-container"
            position={PanelPosition.Left}
            onExpandedStateChanged={e => this.onActivityPickerPanelStateChanged(e.detail)}>
            <elsa-workflow-definition-editor-toolbox ref={el => this.toolbox = el}/>
          </elsa-panel>
          <elsa-canvas
            class="absolute" ref={el => this.canvas = el}
            interactiveMode={true}
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

    return <elsa-workflow-definition-properties-editor
      workflowDefinition={this.workflowDefinitionState}
      onWorkflowPropsUpdated={e => this.onWorkflowPropsUpdated(e)}
    />;
  }

  private getWorkflowDefinitionInternal = async (): Promise<WorkflowDefinition> => {
    const root = await this.canvas.exportGraph();
    const workflowDefinition = this.workflowDefinitionState;
    workflowDefinition.root = root;
    return workflowDefinition;
  };

  private emitActivityChanged = async (activity: Activity, propertyName: string) => {
    await this.eventBus.emit(WorkflowEditorEventTypes.Activity.PropertyChanged, this, activity, propertyName, this);
  };

  private saveChanges = async (): Promise<void> => {
    const workflowDefinition = await this.getWorkflowDefinitionInternal();
    this.workflowUpdated.emit({workflowDefinition});
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

  private generateUniqueActivityName = async (activityDescriptor: ActivityDescriptor): Promise<string> => {
    const activityType = activityDescriptor.activityType;
    const workflowDefinition = await this.getWorkflowDefinitionInternal();
    const root = workflowDefinition.root;
    const activityDescriptors = this.activityDescriptors;
    const graph = walkActivities(root, activityDescriptors);
    const activityNodes = flattenList(graph.children);
    const activityCount = activityNodes.filter(x => x.activity.typeName == activityType).length;
    let counter = activityCount + 1;
    let newName = this.activityNameFormatter.format({activityDescriptor, count: counter, activityNodes});

    while (!!activityNodes.find(x => x.activity.id == newName))
      newName = this.activityNameFormatter.format({activityDescriptor, count: ++counter, activityNodes});

    return newName;
  };

  private onActivityPickerPanelStateChanged = async (e: PanelStateChangedArgs) => await this.updateContainerLayout('activity-picker-closed', e.expanded)
  private onActivityEditorPanelStateChanged = async (e: PanelStateChangedArgs) => await this.updateContainerLayout('object-editor-closed', e.expanded)

  private onDragOver = (e: DragEvent) => {
    e.preventDefault();
  };

  private onDrop = async (e: DragEvent) => {
    const json = e.dataTransfer.getData('activity-descriptor');
    const activityDescriptor: ActivityDescriptor = JSON.parse(json);
    const newName = await this.generateUniqueActivityName(activityDescriptor);

    await this.canvas.addActivity({
      descriptor: activityDescriptor,
      id: newName,
      x: e.pageX,
      y: e.pageY
    });
  };

  private onActivityUpdated = (e: CustomEvent<ActivityUpdatedArgs>) => {
    const updatedActivity = e.detail.activity;
    this.applyActivityChanges(updatedActivity);
    this.emitActivityChangedDebounced({...e.detail, workflowEditor: this.el});
    this.saveChangesDebounced();
  }

  private onWorkflowPropsUpdated = (e: CustomEvent<WorkflowDefinitionPropsUpdatedArgs>) => this.saveChangesDebounced()

  private onDeleteActivityRequested = (e: CustomEvent<DeleteActivityRequestedArgs>) => {
    this.deleteActivity(e.detail.activity);
    this.selectedActivity = null;
  };
}
