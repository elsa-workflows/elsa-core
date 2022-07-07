import {Component, Element, Event, EventEmitter, h, Listen, Method, Prop, State, Watch} from '@stencil/core';
import {camelCase, debounce} from 'lodash';
import {Container} from "typedi";
import {PanelPosition, PanelStateChangedArgs} from '../panel/models';
import {
  Activity,
  ActivityDeletedArgs,
  ActivityDescriptor,
  ActivitySelectedArgs,
  ChildActivitySelectedArgs,
  Container as ContainerActivity,
  ContainerSelectedArgs,
  EditChildActivityArgs,
  GraphUpdatedArgs,
  WorkflowDefinition
} from '../../../models';
import {ActivityIdUpdatedArgs, ActivityUpdatedArgs, DeleteActivityRequestedArgs} from './activity-properties-editor';
import {ActivityDriverRegistry, ActivityNameFormatter, ActivityNode, createActivityMap, createActivityNodeMap, EventBus, flatten, flattenList, PluginRegistry, PortProviderRegistry, walkActivities} from '../../../services';
import {MonacoEditorSettings} from "../../../services/monaco-editor-settings";
import {Flowchart} from "../../activities/flowchart/models";
import {ActivityPropertyChangedEventArgs, WorkflowDefinitionPropsUpdatedArgs, WorkflowDefinitionUpdatedArgs, WorkflowEditorEventTypes} from "./models";
import descriptorsStore from "../../../data/descriptors-store";
import {WorkflowNavigationItem} from "../workflow-navigator/models";
import WorkflowEditorTunnel, {WorkflowDesignerState} from "../state";
import {Hash} from "../../../utils";

@Component({
  tag: 'elsa-workflow-definition-editor',
  styleUrl: 'workflow-definition-editor.scss',
})
export class WorkflowDefinitionEditor {
  @Element() el: HTMLElsaWorkflowDefinitionEditorElement;

  private readonly pluginRegistry: PluginRegistry;
  private readonly eventBus: EventBus;
  private readonly activityNameFormatter: ActivityNameFormatter;
  private readonly portProviderRegistry: PortProviderRegistry;
  private canvas: HTMLElsaCanvasElement;
  private container: HTMLDivElement;
  private toolbox: HTMLElsaWorkflowDefinitionEditorToolboxElement;
  private nodeMap: Hash<Activity> = {};
  private readonly emitActivityChangedDebounced: (e: ActivityPropertyChangedEventArgs) => void;
  private readonly updateModelDebounced: () => void;
  private readonly saveChangesDebounced: () => void;

  constructor() {
    this.eventBus = Container.get(EventBus);
    this.pluginRegistry = Container.get(PluginRegistry);
    this.activityNameFormatter = Container.get(ActivityNameFormatter);
    this.portProviderRegistry = Container.get(PortProviderRegistry);
    this.emitActivityChangedDebounced = debounce(this.emitActivityChanged, 100);
    this.updateModelDebounced = debounce(this.updateModel, 10);
    this.saveChangesDebounced = debounce(this.saveChanges, 1000);
  }

  @Prop() workflowDefinition?: WorkflowDefinition;
  @Prop({attribute: 'monaco-lib-path'}) monacoLibPath: string;
  @Event() workflowUpdated: EventEmitter<WorkflowDefinitionUpdatedArgs>
  @State() private workflowDefinitionState: WorkflowDefinition;
  @State() private selectedActivity?: Activity;
  @State() private currentWorkflowPath: Array<WorkflowNavigationItem> = [];

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

  @Listen('containerSelected')
  private async handleContainerSelected(e: CustomEvent<ContainerSelectedArgs>) {
    this.selectedActivity = this.getCurrentContainer();
  }

  @Listen('activitySelected')
  private async handleActivitySelected(e: CustomEvent<ActivitySelectedArgs>) {
    this.selectedActivity = e.detail.activity;
  }

  @Listen('childActivitySelected')
  private async handleChildActivitySelected(e: CustomEvent<ChildActivitySelectedArgs>) {
    const {parentActivity, childActivity, port} = e.detail;
    this.selectedActivity = childActivity;
    const parentActivityId = parentActivity.id;
  }

  @Listen('activityDeleted')
  private async handleActivityDeleted(e: CustomEvent<ActivityDeletedArgs>) {
    this.selectedActivity = this.getCurrentContainer();
  }

  @Listen('graphUpdated')
  private async handleGraphUpdated(e: CustomEvent<GraphUpdatedArgs>) {
    this.updateModelDebounced();
    this.saveChangesDebounced();
  }

  @Listen('editChildActivity')
  private async editChildActivity(e: CustomEvent<EditChildActivityArgs>) {
    const parentActivityId = e.detail.parentActivityId;
    const currentActivityId = this.currentWorkflowPath[this.currentWorkflowPath.length - 1].activityId;
    const currentActivity = this.nodeMap[currentActivityId];
    const parentActivity = this.nodeMap[parentActivityId];
    const parentActivityDescriptor = descriptorsStore.activityDescriptors.find(x => x.activityType == parentActivity.typeName);
    const indexInParent = currentActivity.activities?.findIndex(x => x == parentActivity);
    const portName = e.detail.port.name;

    const item: WorkflowNavigationItem = {
      activityId: parentActivityId,
      portName: portName,
      index: indexInParent
    };

    const portProvider = this.portProviderRegistry.get(parentActivity.typeName);
    const activityProperty = portProvider.resolvePort(portName, {activity: parentActivity, activityDescriptor: parentActivityDescriptor});
    const isContainer = Array.isArray(activityProperty);

    if (!activityProperty) {
      await this.canvas.reset();
    } else {
      if (isContainer) {
        await this.canvas.importGraph(parentActivity);
      } else {
        await this.canvas.importGraph(activityProperty as Activity);
      }
    }

    this.currentWorkflowPath = [...this.currentWorkflowPath, item];
    this.selectedActivity = this.getCurrentContainer();
  }

  @Method()
  async getCanvas(): Promise<HTMLElsaCanvasElement> {
    return this.canvas;
  }

  @Method()
  async registerActivityDrivers(register: (registry: ActivityDriverRegistry) => void): Promise<void> {
    const registry = Container.get(ActivityDriverRegistry);
    register(registry);
  }

  @Method()
  getWorkflowDefinition(): Promise<WorkflowDefinition> {
    return this.getWorkflowDefinitionInternal();
  }

  @Method()
  async importWorkflow(workflowDefinition: WorkflowDefinition): Promise<void> {
    await this.updateWorkflowDefinition(workflowDefinition);
    await this.canvas.importGraph(workflowDefinition.root);
    await this.eventBus.emit(WorkflowEditorEventTypes.WorkflowDefinition.Imported, this, {workflowDefinition});
  }

  // Updates the workflow definition without importing it into the designer.
  @Method()
  async updateWorkflowDefinition(workflowDefinition: WorkflowDefinition): Promise<void> {
    this.workflowDefinitionState = workflowDefinition;
    this.createNodeMap(workflowDefinition);

    if (this.currentWorkflowPath.length == 0) {
      this.currentWorkflowPath = [{activityId: workflowDefinition.root.id, portName: null, index: 0}];
      this.selectedActivity = this.getCurrentContainer();
    }
  }

  @Method()
  async newWorkflow() {

    const flowchartDescriptor = descriptorsStore.activityDescriptors.find(x => x.activityType == 'Elsa.Flowchart');
    const newName = await this.generateUniqueActivityName(flowchartDescriptor);

    const flowchart = {
      typeName: 'Elsa.Flowchart',
      activities: [],
      connections: [],
      id: newName,
      metadata: {},
      applicationProperties: {},
      variables: []
    } as Flowchart;

    const workflowDefinition: WorkflowDefinition = {
      root: flowchart,
      id: '',
      name: 'Workflow 1',
      definitionId: '',
      version: 1,
      isLatest: true,
      isPublished: false,
      materializerName: 'Json'
    }

    await this.importWorkflow(workflowDefinition);
  }

  async componentWillLoad() {
    await this.updateWorkflowDefinition(this.workflowDefinition);
  }

  async componentDidLoad() {
    if (!this.workflowDefinitionState)
      await this.newWorkflow();
    else
      await this.importWorkflow(this.workflowDefinitionState);

    await this.eventBus.emit(WorkflowEditorEventTypes.WorkflowEditor.Ready, this, {workflowEditor: this});
  }

  render() {

    const workflowDefinition = this.workflowDefinitionState;
    const nodeMap = this.nodeMap;

    const tunnelState: WorkflowDesignerState = {
      workflowDefinition: this.workflowDefinitionState,
      nodeMap: nodeMap
    };

    return (
      <WorkflowEditorTunnel.Provider state={tunnelState}>
        <div class="absolute inset-0" ref={el => this.container = el}>
          <elsa-workflow-definition-editor-toolbar zoomToFit={this.onZoomToFit}/>
          <elsa-workflow-navigator items={this.currentWorkflowPath} workflowDefinition={workflowDefinition} onNavigate={this.onNavigateHierarchy}/>
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
            class="elsa-workflow-editor-container"
            position={PanelPosition.Right}
            onExpandedStateChanged={e => this.onWorkflowEditorPanelStateChanged(e.detail)}>
            <div class="object-editor-container">
              <elsa-workflow-definition-properties-editor
                workflowDefinition={this.workflowDefinitionState}
                onWorkflowPropsUpdated={e => this.onWorkflowPropsUpdated(e)}
              />
            </div>
          </elsa-panel>
          <elsa-panel
            class="elsa-activity-editor-container"
            position={PanelPosition.Bottom}
            onExpandedStateChanged={e => this.onActivityEditorPanelStateChanged(e.detail)}>
            <div class="activity-editor-container">
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
        variables={this.workflowDefinitionState.variables}
        onActivityUpdated={e => this.onActivityUpdated(e)}
        onActivityIdUpdated={e => this.onActivityIdUpdated(e)}/>;
  }

  private getWorkflowDefinitionInternal = async (): Promise<WorkflowDefinition> => {
    const activity: Activity = await this.canvas.exportGraph();
    const workflowDefinition = this.workflowDefinitionState;
    const currentWorkflowPath = this.currentWorkflowPath;
    const currentWorkflowNavigationItem = currentWorkflowPath[currentWorkflowPath.length - 1];
    const currentActivityId = currentWorkflowNavigationItem.activityId;
    const currentPortName = currentWorkflowNavigationItem.portName;
    const currentActivity = this.nodeMap[currentActivityId];
    const currentActivityDescriptor = descriptorsStore.activityDescriptors.find(x => x.activityType == currentActivity.typeName);

    if (!activity.id) {
      const descriptor = descriptorsStore.activityDescriptors.find(x => x.activityType == activity.typeName);
      activity.id = await this.generateUniqueActivityName(descriptor);
    }

    if (!!currentPortName) {
      if (currentActivityDescriptor.isContainer) {
        const parentNavigationItem = this.currentWorkflowPath[this.currentWorkflowPath.length - 2];
        const parentActivityId = parentNavigationItem.activityId;
        const parentActivity = this.nodeMap[parentActivityId] as ContainerActivity;
        const parentActivityDescriptor = descriptorsStore.activityDescriptors.find(x => x.activityType == parentActivity.typeName);
        const portProvider = this.portProviderRegistry.get(parentActivity.typeName);
        const parentActivitiesProp = portProvider.resolvePort(currentPortName, {activity: parentActivity, activityDescriptor: parentActivityDescriptor}) as Array<Activity>;
        parentActivitiesProp[currentWorkflowNavigationItem.index] = activity;
      } else {
        const portProvider = this.portProviderRegistry.get(currentActivity.typeName);
        portProvider.assignPort(currentPortName, activity, {activity: currentActivity, activityDescriptor: currentActivityDescriptor});
      }
    } else {
      workflowDefinition.root = activity;
    }

    this.createNodeMap(workflowDefinition);
    return workflowDefinition;
  };

  private emitActivityChanged = async (activity: Activity, propertyName: string) => {
    await this.eventBus.emit(WorkflowEditorEventTypes.Activity.PropertyChanged, this, activity, propertyName, this);
  };

  private updateModel = async (): Promise<void> => {
    const workflowDefinition = await this.getWorkflowDefinitionInternal();
    await this.updateWorkflowDefinition(workflowDefinition);
  };

  private saveChanges = async (): Promise<void> => {
    this.workflowUpdated.emit({workflowDefinition: this.workflowDefinitionState});
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
    const workflowDefinition = this.workflowDefinitionState;
    const root = workflowDefinition.root;
    const graph = walkActivities(root);
    const activityNodes = flattenList(graph.children);
    const activityCount = activityNodes.filter(x => x.activity.typeName == activityType).length;
    let counter = activityCount + 1;
    let newName = this.activityNameFormatter.format({activityDescriptor, count: counter, activityNodes});

    while (!!activityNodes.find(x => x.activity.id == newName))
      newName = this.activityNameFormatter.format({activityDescriptor, count: ++counter, activityNodes});

    return newName;
  };

  private getCurrentContainer = (): Activity => {
    const currentItem = this.currentWorkflowPath.length > 0 ? this.currentWorkflowPath[this.currentWorkflowPath.length - 1] : null;

    if (!currentItem)
      return this.workflowDefinitionState.root;

    const activity = this.nodeMap[currentItem.activityId];
    const activityDescriptor = descriptorsStore.activityDescriptors.find(x => x.activityType == activity.typeName);

    if (activityDescriptor.isContainer)
      return activity;

    const portProvider = this.portProviderRegistry.get(activity.typeName);
    return portProvider.resolvePort(currentItem.portName, {activity, activityDescriptor}) as Activity;
  };

  private createNodeMap = (workflowDefinition: WorkflowDefinition): void => {
    const nodes = flatten(walkActivities(workflowDefinition.root));
    this.nodeMap = createActivityMap(nodes);
  };

  private onActivityPickerPanelStateChanged = async (e: PanelStateChangedArgs) => await this.updateContainerLayout('activity-picker-closed', e.expanded)
  private onWorkflowEditorPanelStateChanged = async (e: PanelStateChangedArgs) => await this.updateContainerLayout('object-editor-closed', e.expanded)
  private onActivityEditorPanelStateChanged = async (e: PanelStateChangedArgs) => await this.updateContainerLayout('activity-editor-closed', e.expanded)

  private onDragOver = (e: DragEvent) => {
    e.preventDefault();
  };

  private onDrop = async (e: DragEvent) => {
    const json = e.dataTransfer.getData('activity-descriptor');
    const activityDescriptor: ActivityDescriptor = JSON.parse(json);
    const newName = await this.generateUniqueActivityName(activityDescriptor);

    const activity = await this.canvas.addActivity({
      descriptor: activityDescriptor,
      id: newName,
      x: e.pageX,
      y: e.pageY
    });

    this.nodeMap[newName] = activity;
  };

  private onZoomToFit = async () => await this.canvas.zoomToFit()

  private onActivityUpdated = async (e: CustomEvent<ActivityUpdatedArgs>) => {
    const updatedActivity = e.detail.activity;
    this.nodeMap[updatedActivity.id] = updatedActivity;
    await this.canvas.updateActivity({activity: updatedActivity, id: updatedActivity.id});
    await this.updateModel();
    this.emitActivityChangedDebounced({...e.detail, workflowEditor: this.el});
    this.saveChangesDebounced();
  }

  private onActivityIdUpdated = (e: CustomEvent<ActivityIdUpdatedArgs>) => {
    const originalId = e.detail.originalId;
    const newId = e.detail.newId;
    const workflowPath = this.currentWorkflowPath;
    const item = workflowPath.find(x => x.activityId == originalId);

    if (!item)
      return;

    item.activityId = newId;
    this.currentWorkflowPath = [...workflowPath];
    this.createNodeMap(this.workflowDefinition);
  }

  private onWorkflowPropsUpdated = (e: CustomEvent<WorkflowDefinitionPropsUpdatedArgs>) => {
    this.updateModelDebounced();
    this.saveChangesDebounced();
  }

  private onNavigateHierarchy = async (e: CustomEvent<WorkflowNavigationItem>) => {
    const item = e.detail;
    const activityId = item.activityId;
    let activity = this.nodeMap[activityId];
    const activityDescriptor = descriptorsStore.activityDescriptors.find(x => x.activityType == activity.typeName);
    const path = this.currentWorkflowPath;
    const index = path.indexOf(item);

    this.currentWorkflowPath = path.slice(0, index + 1);

    if (!!item.portName) {
      if (!activityDescriptor.isContainer) {
        const portName = camelCase(item.portName);
        activity = activity[portName] as Activity;
      }
    }

    await this.canvas.importGraph(activity);
    this.selectedActivity = this.getCurrentContainer();
  }
}
