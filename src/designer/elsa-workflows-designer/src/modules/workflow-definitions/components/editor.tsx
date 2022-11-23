import {Component, Element, Event, EventEmitter, h, Listen, Method, Prop, State, Watch} from '@stencil/core';
import {debounce} from 'lodash';
import {Container} from "typedi";
import {PanelPosition, PanelStateChangedArgs} from '../../../components/panel/models';
import {
  Activity,
  ActivityDescriptor,
  ActivitySelectedArgs,
  ChildActivitySelectedArgs,
  ContainerSelectedArgs,
  GraphUpdatedArgs
} from '../../../models';
import {ActivityUpdatedArgs} from './activity-properties-editor';
import {ActivityDriverRegistry, ActivityNameFormatter, EventBus, PluginRegistry, PortProviderRegistry} from '../../../services';
import {MonacoEditorSettings} from "../../../services/monaco-editor-settings";
import {ActivityPropertyChangedEventArgs, WorkflowDefinitionPropsUpdatedArgs, WorkflowDefinitionUpdatedArgs, WorkflowEditorEventTypes} from "../models/ui";
import {WorkflowDefinition} from "../models/entities";
import {WorkflowDefinitionsApi} from "../services/api"
import WorkflowDefinitionEditorTunnel, {WorkflowDefinitionEditorState} from "../state";

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
  private canvas: HTMLElsaCanvasElement;
  private container: HTMLDivElement;
  private toolbox: HTMLElsaWorkflowDefinitionEditorToolboxElement;
  private readonly emitActivityChangedDebounced: (e: ActivityPropertyChangedEventArgs) => void;
  private readonly updateModelDebounced: () => void;
  private readonly saveChangesDebounced: () => void;
  private readonly workflowDefinitionApi: WorkflowDefinitionsApi;

  constructor() {
    this.eventBus = Container.get(EventBus);
    this.pluginRegistry = Container.get(PluginRegistry);
    this.activityNameFormatter = Container.get(ActivityNameFormatter);
    this.portProviderRegistry = Container.get(PortProviderRegistry);
    this.emitActivityChangedDebounced = debounce(this.emitActivityChanged, 100);
    this.updateModelDebounced = debounce(this.updateModel, 10);
    this.saveChangesDebounced = debounce(this.saveChanges, 1000);
    this.workflowDefinitionApi = Container.get(WorkflowDefinitionsApi);
  }

  @Prop() workflowDefinition?: WorkflowDefinition;
  @Prop({attribute: 'monaco-lib-path'}) monacoLibPath: string;
  @Event() workflowUpdated: EventEmitter<WorkflowDefinitionUpdatedArgs>
  @State() private workflowDefinitionState: WorkflowDefinition;
  @State() private selectedActivity?: Activity;
  @State() workflowVersions: Array<WorkflowDefinition> = [];

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

  @Listen('activitySelected')
  private async handleActivitySelected(e: CustomEvent<ActivitySelectedArgs>) {
    this.selectedActivity = e.detail.activity;
  }

  @Listen('childActivitySelected')
  private async handleChildActivitySelected(e: CustomEvent<ChildActivitySelectedArgs>) {
    const {childActivity} = e.detail;
    this.selectedActivity = childActivity;
  }

  @Listen('graphUpdated')
  private async handleGraphUpdated(e: CustomEvent<GraphUpdatedArgs>) {
    this.updateModelDebounced();
    this.saveChangesDebounced();
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
    await this.canvas.scrollToStart();
  }

  // Updates the workflow definition without importing it into the designer.
  @Method()
  async updateWorkflowDefinition(workflowDefinition: WorkflowDefinition): Promise<void> {
    this.workflowDefinitionState = workflowDefinition;
  }

  @Method()
  async newWorkflow(): Promise<WorkflowDefinition> {

    const newRoot = await this.canvas.newRoot();

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

  async componentWillLoad() {
    await this.updateWorkflowDefinition(this.workflowDefinition);
    await this.loadWorkflowVersions();
  }

  async componentDidLoad() {
    if (!this.workflowDefinitionState)
      await this.newWorkflow();
    else
      await this.importWorkflow(this.workflowDefinitionState);

    await this.eventBus.emit(WorkflowEditorEventTypes.WorkflowEditor.Ready, this, {workflowEditor: this});
  }

  private renderSelectedObject = () => {
    if (!!this.selectedActivity)
      return <elsa-activity-properties-editor
        activity={this.selectedActivity}
        variables={this.workflowDefinitionState.variables}
        onActivityUpdated={e => this.onActivityUpdated(e)}/>;
  }

  private getWorkflowDefinitionInternal = async (): Promise<WorkflowDefinition> => {
    const activity: Activity = await this.canvas.exportGraph();
    const workflowDefinition = this.workflowDefinitionState;
    workflowDefinition.root = activity;
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
    const latestVersion = this.workflowVersions.find(v => v.isLatest);
    this.workflowUpdated.emit({workflowDefinition: this.workflowDefinitionState, latestVersionNumber: latestVersion?.version});
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
  private onWorkflowEditorPanelStateChanged = async (e: PanelStateChangedArgs) => await this.updateContainerLayout('object-editor-closed', e.expanded)
  private onActivityEditorPanelStateChanged = async (e: PanelStateChangedArgs) => await this.updateContainerLayout('activity-editor-closed', e.expanded)

  private onDragOver = (e: DragEvent) => {
    e.preventDefault();
  };

  private onDrop = async (e: DragEvent) => {
    const json = e.dataTransfer.getData('activity-descriptor');
    const activityDescriptor: ActivityDescriptor = JSON.parse(json);

    await this.canvas.addActivity({
      descriptor: activityDescriptor,
      x: e.pageX,
      y: e.pageY
    });
  };

  private onZoomToFit = async () => await this.canvas.zoomToFit()

  private onAutoLayout = async () => await this.canvas.autoLayout()

  private onActivityUpdated = async (e: CustomEvent<ActivityUpdatedArgs>) => {
    await this.canvas.updateActivity({
      id: e.detail.newId,
      originalId: e.detail.originalId,
      activity: e.detail.activity
    });

    await this.updateModel();
    this.emitActivityChangedDebounced({...e.detail, workflowEditor: this.el});
    this.saveChangesDebounced();
  }

  private onWorkflowPropsUpdated = (e: CustomEvent<WorkflowDefinitionPropsUpdatedArgs>) => {
    this.updateModelDebounced();
    this.saveChangesDebounced();
  }

  onVersionSelected = async (e: CustomEvent<WorkflowDefinition>) => {
    const workflowToView = e.detail;
    const workflowDefinition = await this.workflowDefinitionApi.get({definitionId: workflowToView.definitionId, versionOptions: {version: workflowToView.version}});
    await this.importWorkflow(workflowDefinition);
  };

  onDeleteVersionClicked = async (e: CustomEvent<WorkflowDefinition>) => {
    const workflowToDelete = e.detail;
    await this.workflowDefinitionApi.deleteVersion({definitionId: workflowToDelete.definitionId, version: workflowToDelete.version});
    const latestWorkflowDefinition = await this.workflowDefinitionApi.get({definitionId: workflowToDelete.definitionId, versionOptions: {isLatest: true}});
    await this.loadWorkflowVersions();
    await this.importWorkflow(latestWorkflowDefinition);
  };

  onRevertVersionClicked = async (e: CustomEvent<WorkflowDefinition>) => {
    const workflowToRevert = e.detail;
    await this.workflowDefinitionApi.revertVersion({definitionId: workflowToRevert.definitionId, version: workflowToRevert.version});
    const workflowDefinition = await this.workflowDefinitionApi.get({definitionId: workflowToRevert.definitionId, versionOptions: {isLatest: true}});
    await this.loadWorkflowVersions();
    await this.importWorkflow(workflowDefinition);
  };

  render() {
    const state: WorkflowDefinitionEditorState = {
      workflowDefinition: this.workflowDefinition
    };

    return (
      <WorkflowDefinitionEditorTunnel.Provider state={state}>
        <div class="absolute inset-0" ref={el => this.container = el}>
          <elsa-workflow-definition-editor-toolbar zoomToFit={this.onZoomToFit} autoLayout={this.onAutoLayout}/>
          <elsa-panel
            class="elsa-activity-picker-container z-30"
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
      </WorkflowDefinitionEditorTunnel.Provider>
    );
  }
}
