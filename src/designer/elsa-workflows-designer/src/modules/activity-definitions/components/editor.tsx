import {Component, Element, Event, EventEmitter, h, Listen, Method, Prop, State, Watch} from '@stencil/core';
import {debounce} from 'lodash';
import {Container} from "typedi";
import {PanelPosition, PanelStateChangedArgs} from '../../../components/panel/models';
import {
  Activity,
  ActivityDeletedArgs,
  ActivityDescriptor,
  ActivitySelectedArgs,
  ChildActivitySelectedArgs,
  ContainerSelectedArgs,
  GraphUpdatedArgs,
} from '../../../models';
import {ActivityDriverRegistry, ActivityNameFormatter, EventBus, PluginRegistry, PortProviderRegistry} from '../../../services';
import {MonacoEditorSettings} from "../../../services/monaco-editor-settings";
import {ActivityPropertyChangedEventArgs} from "../../workflow-definitions/models/ui";
import {ActivityDefinition, ActivityDefinitionPropsUpdatedArgs, ActivityDefinitionUpdatedArgs} from "../models";
import {ActivityIdUpdatedArgs, ActivityUpdatedArgs} from "../../workflow-definitions/components/activity-properties-editor";

@Component({
  tag: 'elsa-activity-definition-editor',
  styleUrl: 'editor.scss',
})
export class Editor {
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

  constructor() {
    this.eventBus = Container.get(EventBus);
    this.pluginRegistry = Container.get(PluginRegistry);
    this.activityNameFormatter = Container.get(ActivityNameFormatter);
    this.portProviderRegistry = Container.get(PortProviderRegistry);
    this.emitActivityChangedDebounced = debounce(this.emitActivityChanged, 100);
    this.updateModelDebounced = debounce(this.updateModel, 10);
    this.saveChangesDebounced = debounce(this.saveChanges, 1000);
  }

  @Prop() activityDefinition?: ActivityDefinition;
  @Prop({attribute: 'monaco-lib-path'}) monacoLibPath: string;
  @Event() activityDefinitionUpdated: EventEmitter<ActivityDefinitionUpdatedArgs>
  @State() private activityDefinitionState: ActivityDefinition;
  @State() private selectedActivity?: Activity;

  @Watch('monacoLibPath')
  private handleMonacoLibPath(value: string) {
    const settings = Container.get(MonacoEditorSettings);
    settings.monacoLibPath = value;
  }

  @Watch('activityDefinition')
  async onActivityDefinitionChanged(value: ActivityDefinition) {
    await this.importDefinition(value);
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
    const {parentActivity, childActivity, port} = e.detail;
    this.selectedActivity = childActivity;
    const parentActivityId = parentActivity.id;
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
  getActivityDefinition(): Promise<ActivityDefinition> {
    return this.getActivityDefinitionInternal();
  }

  @Method()
  async importDefinition(activityDefinition: ActivityDefinition): Promise<void> {
    await this.updateActivityDefinition(activityDefinition);
    await this.canvas.importGraph(activityDefinition.root);
  }

  // Updates the workflow definition without importing it into the designer.
  @Method()
  async updateActivityDefinition(activityDefinition: ActivityDefinition): Promise<void> {
    this.activityDefinitionState = activityDefinition;
  }

  @Method()
  async newActivityDefinition(): Promise<ActivityDefinition> {

    const newRoot = await this.canvas.newRoot();

    const activityDefinition: ActivityDefinition = {
      root: newRoot,
      id: '',
      typeName: 'Activity1',
      displayName: 'Activity 1',
      category: 'Custom',
      definitionId: '',
      version: 1,
      isLatest: true,
      isPublished: false,
    }

    await this.updateActivityDefinition(activityDefinition);
    return activityDefinition;
  }

  async componentWillLoad() {
    await this.updateActivityDefinition(this.activityDefinition);
  }

  async componentDidLoad() {
    if (!this.activityDefinitionState)
      await this.newActivityDefinition();
    else
      await this.importDefinition(this.activityDefinitionState);
  }

  private renderSelectedObject = () => {
    if (!!this.selectedActivity)
      return <elsa-activity-properties-editor
        activity={this.selectedActivity}
        variables={this.activityDefinitionState.variables}
        onActivityUpdated={e => this.onActivityUpdated(e)}/>;
  }

  private getActivityDefinitionInternal = async (): Promise<ActivityDefinition> => {
    const activity: Activity = await this.canvas.exportGraph();
    const activityDefinition = this.activityDefinitionState;
    activityDefinition.root = activity;
    return activityDefinition;
  };

  private emitActivityChanged = async (activity: Activity, propertyName: string) => {
    //await this.eventBus.emit(WorkflowEditorEventTypes.Activity.PropertyChanged, this, activity, propertyName, this);
  };

  private updateModel = async (): Promise<void> => {
    const workflowDefinition = await this.getActivityDefinitionInternal();
    await this.updateActivityDefinition(workflowDefinition);
  };

  private saveChanges = async (): Promise<void> => {
    this.activityDefinitionUpdated.emit({activityDefinition: this.activityDefinitionState});
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

  private onActivityUpdated = async (e: CustomEvent<ActivityUpdatedArgs>) => {
    await this.canvas.updateActivity({
      id: e.detail.newId,
      originalId: e.detail.originalId,
      activity: e.detail.activity,
    });

    await this.updateModel();
    this.emitActivityChangedDebounced({...e.detail, workflowEditor: this.el});
    this.saveChangesDebounced();
  }

  private onActivityDefinitionPropsUpdated = (e: CustomEvent<ActivityDefinitionPropsUpdatedArgs>) => {
    this.updateModelDebounced();
    this.saveChangesDebounced();
  }

  render() {

    return (
      <div class="absolute inset-0" ref={el => this.container = el}>
        <elsa-workflow-definition-editor-toolbar zoomToFit={this.onZoomToFit}/>
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
            <elsa-activity-definition-properties-editor
              activityDefinition={this.activityDefinitionState}
              onActivityDefinitionPropsUpdated={e => this.onActivityDefinitionPropsUpdated(e)}
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
    );
  }
}
