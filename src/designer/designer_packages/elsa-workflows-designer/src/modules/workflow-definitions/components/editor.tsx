import {Component, Element, Event, EventEmitter, h, Listen, Method, Prop, State, Watch} from '@stencil/core';
import {debounce, isEqual} from 'lodash';
import {Container} from "typedi";
import {PanelPosition, PanelStateChangedArgs} from '../../../components/panel/models';
import {
  Activity,
  ActivityDescriptor,
  ActivitySelectedArgs,
  ChildActivitySelectedArgs,
  ContainerSelectedArgs, GraphUpdatedArgs
} from '../../../models';
import {ActivityDriverRegistry, ActivityNameFormatter, EventBus, PluginRegistry, PortProviderRegistry} from '../../../services';
import {MonacoEditorSettings} from "../../../services/monaco-editor-settings";
import {WorkflowDefinitionPropsUpdatedArgs, WorkflowDefinitionUpdatedArgs, ActivityUpdatedArgs, WorkflowEditorEventTypes} from "../models/ui";
import {WorkflowDefinition} from "../models/entities";
import {WorkflowDefinitionsApi} from "../services/api"
import WorkflowDefinitionTunnel, {WorkflowDefinitionState} from "../state";
import {LayoutDirection, UpdateActivityArgs} from "../../flowchart/models";
import {cloneDeep} from '@antv/x6/lib/util/object/object';
import {removeGuidsFromPortNames} from '../../../utils/graph';
import { WorkflowPropertiesEditorTabs } from '../models/props-editor-tabs';

@Component({
  tag: 'elsa-workflow-definition-editor',
  styleUrl: 'editor.scss',
})
export class WorkflowDefinitionEditor {
  @Element() el: HTMLElsaWorkflowDefinitionEditorElement;

  private readonly pluginRegistry: PluginRegistry;
  private readonly eventBus: EventBus;
  private readonly activityNameFormatter: ActivityNameFormatter;
  private readonly portProviderRegistry: PortProviderRegistry;
  private flowchart: HTMLElsaFlowchartElement;
  private container: HTMLDivElement;
  private toolbox: HTMLElsaWorkflowDefinitionEditorToolboxElement;
  private readonly saveChangesDebounced: () => void;
  private readonly workflowDefinitionApi: WorkflowDefinitionsApi;

  constructor() {
    this.eventBus = Container.get(EventBus);
    this.pluginRegistry = Container.get(PluginRegistry);
    this.activityNameFormatter = Container.get(ActivityNameFormatter);
    this.portProviderRegistry = Container.get(PortProviderRegistry);
    this.saveChangesDebounced = debounce(this.saveChanges, 1000);
    this.workflowDefinitionApi = Container.get(WorkflowDefinitionsApi);
  }

  @Prop() workflowDefinition?: WorkflowDefinition;
  @Prop({attribute: 'monaco-lib-path'}) monacoLibPath: string;
  @Event() workflowUpdated: EventEmitter<WorkflowDefinitionUpdatedArgs>
  @State() private workflowDefinitionState: WorkflowDefinition;
  @State() private selectedActivity?: Activity;
  @State() private workflowVersions: Array<WorkflowDefinition> = [];

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
    this.selectedActivity = e.detail.activity;
  }

  @Method()
  async getFlowchart(): Promise<HTMLElsaFlowchartElement> {
    return this.flowchart;
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
    await this.loadWorkflowVersions();

    // Update the flowchart after state is updated.
    window.requestAnimationFrame(async () => {
      await this.flowchart.updateGraph();
      await this.updateSelectedActivity();
    });

    await this.eventBus.emit(WorkflowEditorEventTypes.WorkflowDefinition.Imported, this, {workflowDefinition});
  }

  // Updates the workflow definition without importing it into the designer.
  @Method()
  async updateWorkflowDefinition(workflowDefinition: WorkflowDefinition): Promise<void> {
    if(this.workflowDefinitionState != workflowDefinition) {
      this.workflowDefinitionState = workflowDefinition;

      window.requestAnimationFrame(async () => {
        await this.updateSelectedActivity();
        await this.eventBus.emit(WorkflowEditorEventTypes.WorkflowEditor.WorkflowLoaded, this, {workflowEditor: this, workflowDefinition: workflowDefinition});
      });
    }
  }

  @Method()
  async newWorkflow(): Promise<WorkflowDefinition> {

    const newRoot = await this.flowchart.newRoot();

    const workflowDefinition: WorkflowDefinition = {
      root: newRoot,
      id: '',
      name: 'Workflow 1',
      definitionId: '',
      version: 1,
      isLatest: true,
      isPublished: false,
      materializerName: 'Json'
    }

    await this.updateWorkflowDefinition(workflowDefinition);
    return workflowDefinition;
  }

  @Method()
  async loadWorkflowVersions(): Promise<void> {
    if (this.workflowDefinitionState.definitionId != null && this.workflowDefinitionState.definitionId.length > 0) {
      const workflowVersions = await this.workflowDefinitionApi.getVersions(this.workflowDefinitionState.definitionId);
      this.workflowVersions = workflowVersions.sort(x => x.version).reverse();
    } else {
      this.workflowVersions = [];
    }
  }

  @Method()
  async updateActivity(activity: Activity) {
    const args: UpdateActivityArgs = {
      activity: activity,
      id: activity.id,
      originalId: activity.id
    };
    await this.updateActivityInternal(args);
  }

  async componentWillLoad() {
    await this.updateWorkflowDefinition(this.workflowDefinition);
    await this.loadWorkflowVersions();
  }

  async componentDidLoad() {
    if (!this.workflowDefinitionState)
      await this.newWorkflow();
    else
      await this.importWorkflow(this.workflowDefinitionState);

    await this.eventBus.emit(WorkflowEditorEventTypes.WorkflowEditor.Ready, this, {workflowEditor: this, workflowDefinition: this.workflowDefinitionState});
  }

  private renderSelectedObject = () => {
    if (!!this.selectedActivity)
      return <elsa-activity-properties-editor
        activity={this.selectedActivity}
        variables={this.workflowDefinitionState.variables}
        outputs={this.workflowDefinitionState.outputs}
        workflowDefinitionId={this.workflowDefinitionState.definitionId}
        onActivityUpdated={e => this.onActivityUpdated(e)}/>;
  }

  private getWorkflowDefinitionInternal = async (): Promise<WorkflowDefinition> => {
    const activity: Activity = await this.flowchart.export();
    const workflowDefinition = this.workflowDefinitionState;
    workflowDefinition.root = activity;
    return workflowDefinition;
  };

  private saveChanges = async (): Promise<void> => {
    const updatedWorkflowDefinition = this.workflowDefinitionState;

    if(!updatedWorkflowDefinition.isLatest) {
      console.debug('Workflow definition is not the latest version. Changes will not be saved.');
      return;
    }

    if (await this.hasWorkflowDefinitionAnyUpdatedData(updatedWorkflowDefinition)) {
      // If workflow definition is published, override the latest version.
      if (updatedWorkflowDefinition.isPublished) {
        updatedWorkflowDefinition.version = this.workflowVersions.find(v => v.isLatest).version;
      }
      this.workflowUpdated.emit({workflowDefinition: this.workflowDefinitionState});
    }

    await this.updateSelectedActivity();
  };

  // To prevent redundant post requests to server, save changes only if there is a difference
  // between existing workflow definition on server side and updated workflow definition on client side.
  private hasWorkflowDefinitionAnyUpdatedData = async (updatedWorkflowDefinition: WorkflowDefinition): Promise<boolean> => {
        const existingWorkflowDefinition = await this.workflowDefinitionApi.get({definitionId: updatedWorkflowDefinition.definitionId, versionOptions: {version: updatedWorkflowDefinition.version}});
    const updatedWorkflowDefinitionClone = cloneDeep(updatedWorkflowDefinition);

    removeGuidsFromPortNames(updatedWorkflowDefinitionClone.root);

    return !isEqual(existingWorkflowDefinition, updatedWorkflowDefinitionClone);
  }

  private updateLayout = async () => {
    await this.flowchart.updateLayout();
  };

  private updateContainerLayout = async (panelClassName: string, panelExpanded: boolean) => {

    if (panelExpanded)
      this.container.classList.remove(panelClassName);
    else
      this.container.classList.toggle(panelClassName, true);

    await this.updateLayout();
  }

  private updateActivityInternal = async (args: UpdateActivityArgs) => {
    args.updatePorts = true; // TODO: Make this configurable from a activity plugin.
    await this.flowchart.updateActivity(args);
    this.saveChangesDebounced();
  }

  private onActivityPickerPanelStateChanged = async (e: PanelStateChangedArgs) => await this.updateContainerLayout('activity-picker-closed', e.expanded)
  private onWorkflowEditorPanelStateChanged = async (e: PanelStateChangedArgs) => await this.updateContainerLayout('object-editor-closed', e.expanded)
  private onActivityEditorPanelStateChanged = async (e: PanelStateChangedArgs) => await this.updateContainerLayout('activity-editor-closed', e.expanded)

  private onDragOver = (e: DragEvent) => {
    e.preventDefault();
  };

  private onDrop = async (e: DragEvent) => {
    const json = e.dataTransfer.getData('activity-descriptor');
    const activityDescriptor: ActivityDescriptor = JSON.parse(json);

    await this.flowchart.addActivity({
      descriptor: activityDescriptor,
      x: e.pageX,
      y: e.pageY
    });
  };

  private onZoomToFit = async () => await this.flowchart.zoomToFit();

  private onAutoLayout = async (direction: LayoutDirection) => {
    await this.flowchart.autoLayout(direction);
  };

  private onActivityUpdated = async (e: CustomEvent<ActivityUpdatedArgs>) => {
    const args: UpdateActivityArgs = {
      activity: e.detail.activity,
      id: e.detail.newId ?? e.detail.originalId ?? e.detail.activity.id,
      originalId: e.detail.originalId ?? e.detail.activity.id
    };

    await this.updateActivityInternal(args);
  }

  private onWorkflowPropsUpdated = (e: CustomEvent<WorkflowDefinitionPropsUpdatedArgs>) => {
    this.saveChangesDebounced();

    if(e.detail.updatedTab == WorkflowPropertiesEditorTabs.Variables){
      const currentSelectedActivity = this.selectedActivity;
      this.selectedActivity = null;
      this.selectedActivity = currentSelectedActivity;
    }
  }

  private async onActivitySelected(e: CustomEvent<ActivitySelectedArgs>) {
    this.selectedActivity = e.detail.activity;
  }

  private async onChildActivitySelected(e: CustomEvent<ChildActivitySelectedArgs>) {
    const {childActivity} = e.detail;
    this.selectedActivity = childActivity;
  }

  private async onGraphUpdated(e: CustomEvent<GraphUpdatedArgs>) {
    await this.updateSelectedActivity();
    this.saveChangesDebounced();
  }

  private async updateSelectedActivity() {
    if (!!this.selectedActivity)
      this.selectedActivity = await this.flowchart.getActivity(this.selectedActivity.id);
  }

  private onVersionSelected = async (e: CustomEvent<WorkflowDefinition>) => {
    const workflowToView = e.detail;
    const workflowDefinition = await this.workflowDefinitionApi.get({definitionId: workflowToView.definitionId, versionOptions: {version: workflowToView.version}});
    await this.importWorkflow(workflowDefinition);
  };

  private onDeleteVersionClicked = async (e: CustomEvent<WorkflowDefinition>) => {
    const workflowToDelete = e.detail;
    await this.workflowDefinitionApi.deleteVersion({definitionId: workflowToDelete.definitionId, version: workflowToDelete.version});
    const latestWorkflowDefinition = await this.workflowDefinitionApi.get({definitionId: workflowToDelete.definitionId, versionOptions: {isLatest: true}});
    await this.loadWorkflowVersions();
    await this.importWorkflow(latestWorkflowDefinition);
  };

  private onRevertVersionClicked = async (e: CustomEvent<WorkflowDefinition>) => {
    const workflowToRevert = e.detail;
    await this.workflowDefinitionApi.revertVersion({definitionId: workflowToRevert.definitionId, version: workflowToRevert.version});
    const workflowDefinition = await this.workflowDefinitionApi.get({definitionId: workflowToRevert.definitionId, versionOptions: {isLatest: true}});
    await this.loadWorkflowVersions();
    await this.importWorkflow(workflowDefinition);
  };

  render() {
    const workflowDefinition = this.workflowDefinitionState;

    const state: WorkflowDefinitionState = {
      workflowDefinition: this.workflowDefinitionState
    };

    return (
      <WorkflowDefinitionTunnel.Provider state={state}>
        <div class="absolute inset-0" ref={el => this.container = el}>
          <elsa-workflow-definition-editor-toolbar zoomToFit={this.onZoomToFit} onAutoLayout={(e: CustomEvent<LayoutDirection>) => this.onAutoLayout(e.detail)}/>
          <elsa-panel
            class="elsa-activity-picker-container z-30"
            position={PanelPosition.Left}
            onExpandedStateChanged={e => this.onActivityPickerPanelStateChanged(e.detail)}>
            <elsa-workflow-definition-editor-toolbox ref={el => this.toolbox = el}/>
          </elsa-panel>
          <elsa-flowchart
            ref={el => this.flowchart = el}
            rootActivity={workflowDefinition.root}
            interactiveMode={true}
            onActivitySelected={e => this.onActivitySelected(e)}
            onChildActivitySelected={e => this.onChildActivitySelected(e)}
            onGraphUpdated={e => this.onGraphUpdated(e)}
            onDragOver={e => this.onDragOver(e)}
            onDrop={e => this.onDrop(e)}/>
          <elsa-panel
            class="elsa-workflow-editor-container z-30"
            position={PanelPosition.Right}
            onExpandedStateChanged={e => this.onWorkflowEditorPanelStateChanged(e.detail)}>
            <div class="object-editor-container">
              <elsa-workflow-definition-properties-editor
                workflowDefinition={this.workflowDefinitionState}
                workflowVersions={this.workflowVersions}
                onWorkflowPropsUpdated={e => this.onWorkflowPropsUpdated(e)}
                onVersionSelected={e => this.onVersionSelected(e)}
                onDeleteVersionClicked={e => this.onDeleteVersionClicked(e)}
                onRevertVersionClicked={e => this.onRevertVersionClicked(e)}
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
      </WorkflowDefinitionTunnel.Provider>
    );
  }
}
