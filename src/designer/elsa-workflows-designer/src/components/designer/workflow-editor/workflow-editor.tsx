import {Component, h, Listen, Prop, State, Event, EventEmitter, Method, Watch} from '@stencil/core';
import {debounce} from 'lodash';
import {v4 as uuid} from 'uuid';
import {Container} from "typedi";
import {PanelPosition, PanelStateChangedArgs} from '../panel/models';
import {
  Activity,
  ActivityDescriptor,
  ActivitySelectedArgs, ContainerSelectedArgs,
  GraphUpdatedArgs, Trigger,
  TriggerDescriptor,
  Workflow, WorkflowInstance
} from '../../../models';
import WorkflowEditorTunnel, {WorkflowDesignerState} from '../state';
import {
  ActivityUpdatedArgs,
  DeleteActivityRequestedArgs
} from '../activity-properties-editor/activity-properties-editor';
import {ActivityDriverRegistry} from '../../../services';
import {TriggerSelectedArgs, TriggersUpdatedArgs} from '../trigger-container/trigger-container';
import {DeleteTriggerRequestedArgs, TriggerUpdatedArgs} from "../trigger-properties-editor/trigger-properties-editor";
import {WorkflowPropsUpdatedArgs} from "../workflow-properties-editor/workflow-properties-editor";

export interface WorkflowUpdatedArgs {
  workflow: Workflow;
}

@Component({
  tag: 'elsa-workflow-editor',
  styleUrl: 'workflow-editor.scss',
})
export class WorkflowEditor {

  private canvas: HTMLElsaCanvasElement;
  private container: HTMLDivElement;
  private toolbox: HTMLElsaToolboxElement;
  private triggerContainer: HTMLElsaTriggerContainerElement;
  private applyActivityChanges: (activity: Activity) => void;
  private deleteActivity: (activity: Activity) => void;
  private applyTriggerChanges: (trigger: Trigger) => void;
  private deleteTrigger: (trigger: Trigger) => void;
  private readonly saveChangesDebounced: () => void;

  private workflow: Workflow = {
    root: null,
    identity: {id: uuid(), definitionId: uuid(), version: 1},
    publication: {isLatest: true, isPublished: false},
    metadata: {},
    triggers: []
  };

  private workflowInstance?: WorkflowInstance;

  constructor() {
    this.saveChangesDebounced = debounce(this.saveChanges, 1000);
  }

  @Prop() public activityDescriptors: Array<ActivityDescriptor> = [];
  @Prop() public triggerDescriptors: Array<TriggerDescriptor> = [];
  @Event() public workflowUpdated: EventEmitter<WorkflowUpdatedArgs>
  @State() private selectedActivity?: Activity;
  @State() private selectedTrigger?: Trigger;
  @State() private triggers: Array<Trigger> = [];

  private get interactiveMode(): boolean {
    return !this.workflowInstance;
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
  async handleContainerSelected(e: CustomEvent<ContainerSelectedArgs>) {
    this.selectedActivity = null;
    this.selectedTrigger = null;
    await this.triggerContainer.deselectAll();
  }

  @Listen('activitySelected')
  async handleActivitySelected(e: CustomEvent<ActivitySelectedArgs>) {
    this.selectedActivity = e.detail.activity;
    this.selectedTrigger = null;
    await this.triggerContainer.deselectAll();
    this.applyActivityChanges = e.detail.applyChanges;
    this.deleteActivity = e.detail.deleteActivity;
  }

  @Listen('triggerSelected')
  async handleTriggerSelected(e: CustomEvent<TriggerSelectedArgs>) {
    this.selectedTrigger = e.detail.trigger;
    this.selectedActivity = null;
    this.applyTriggerChanges = e.detail.applyChanges;
    this.deleteTrigger = e.detail.deleteTrigger;
  }

  @Listen('triggerDeselected')
  async handleTriggerDeselected(e: CustomEvent<TriggerSelectedArgs>) {
    this.selectedTrigger = null;
  }

  @Listen('graphUpdated')
  handleGraphUpdated(e: CustomEvent<GraphUpdatedArgs>) {
    if (this.interactiveMode)
      this.saveChangesDebounced();
  }

  @Method()
  async registerActivityDrivers(register: (registry: ActivityDriverRegistry) => void): Promise<void> {
    const registry = Container.get(ActivityDriverRegistry);
    register(registry);
  }

  @Method() getWorkflow(): Promise<Workflow> {
    return this.getWorkflowInternal();
  }

  @Method()
  async importWorkflow(workflow: Workflow, workflowInstance?: WorkflowInstance): Promise<void> {
    this.workflowInstance = workflowInstance;
    await this.importWorkflowMetadata(workflow);
    await this.canvas.importGraph(workflow.root);
  }

  @Method()
  async importWorkflowMetadata(workflow: Workflow): Promise<void> {
    this.workflow = workflow;
    this.triggers = workflow.triggers;
  }

  public render() {
    const tunnelState: WorkflowDesignerState = {
      workflow: this.workflow,
      activityDescriptors: this.activityDescriptors,
      triggerDescriptors: this.triggerDescriptors
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
          <elsa-panel
            class="elsa-trigger-container"
            onExpandedStateChanged={e => this.onTriggerContainerPanelStateChanged(e.detail)}
            position={PanelPosition.Top}>
            <elsa-trigger-container
              interactiveMode={interactiveMode}
              triggerDescriptors={this.triggerDescriptors}
              triggers={this.triggers}
              onTriggersUpdated={e => this.onTriggersUpdated(e.detail)}
              ref={el => this.triggerContainer = el}/>
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
    if (!!this.selectedTrigger)
      return <elsa-trigger-properties-editor
        trigger={this.selectedTrigger}
        onTriggerUpdated={e => this.onTriggerUpdated(e)}
        onDeleteTriggerRequested={e => this.onDeleteTriggerRequested(e)}/>

    if (!!this.selectedActivity)
      return <elsa-activity-properties-editor
        activity={this.selectedActivity}
        onActivityUpdated={e => this.onActivityUpdated(e)}
        onDeleteActivityRequested={e => this.onDeleteActivityRequested(e)}/>

    return <elsa-workflow-properties-editor
      workflow={this.workflow}
      onWorkflowPropsUpdated={e => this.onWorkflowPropsUpdated(e)}/>;
  }

  private getWorkflowInternal = async (): Promise<Workflow> => {
    const root = await this.canvas.exportGraph();
    const workflow = this.workflow;

    workflow.root = root;
    return workflow;
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
  private onTriggerContainerPanelStateChanged = async (e: PanelStateChangedArgs) => await this.updateContainerLayout('trigger-container-closed', e.expanded)
  private onActivityEditorPanelStateChanged = async (e: PanelStateChangedArgs) => await this.updateContainerLayout('object-editor-closed', e.expanded)

  private onTriggersUpdated = async (e: TriggersUpdatedArgs) => {
    this.workflow.triggers = e.triggers;
    await this.saveChangesDebounced();
  };

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
    this.saveChangesDebounced();
  }

  private onTriggerUpdated = (e: CustomEvent<TriggerUpdatedArgs>) => {
    const updatedTrigger = e.detail.trigger;
    this.applyTriggerChanges(updatedTrigger);
    this.saveChangesDebounced();
  }

  private onWorkflowPropsUpdated = (e: CustomEvent<WorkflowPropsUpdatedArgs>) => this.saveChangesDebounced()

  private onDeleteActivityRequested = (e: CustomEvent<DeleteActivityRequestedArgs>) => {
    this.deleteActivity(e.detail.activity);
    this.selectedActivity = null;
  };

  private onDeleteTriggerRequested = (e: CustomEvent<DeleteTriggerRequestedArgs>) => {
    this.deleteTrigger(e.detail.trigger);
    this.selectedTrigger = null;
  };
}
