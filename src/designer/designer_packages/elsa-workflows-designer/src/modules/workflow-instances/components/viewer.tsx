import {Component, h, Listen, Prop, State, Method, Watch, Element} from '@stencil/core';
import {Container} from "typedi";
import {PanelPosition, PanelStateChangedArgs} from '../../../components/panel/models';
import {
  Activity,
  ActivitySelectedArgs,
  ContainerSelectedArgs,
  GraphUpdatedArgs,
  WorkflowInstance,
  WorkflowExecutionLogRecord,
  Workflow
} from '../../../models';
import {PluginRegistry, ActivityNameFormatter, ActivityDriverRegistry, EventBus, ActivityNode} from '../../../services';
import {MonacoEditorSettings} from "../../../services/monaco-editor-settings";
import {WorkflowDefinition} from "../../workflow-definitions/models/entities";
import {WorkflowEditorEventTypes} from "../../workflow-definitions/models/ui";
import { JournalItemSelectedArgs } from '../events';
import {JournalApi} from "../services/journal-api";
import {WorkflowDefinitionsApi} from "../../workflow-definitions/services/api"
import { Flowchart } from '../../flowchart/models';
import { WorkflowDefinitionActivity } from '../../workflow-definitions/components/models';

@Component({
  tag: 'elsa-workflow-instance-viewer',
  styleUrl: 'viewer.scss',
})
export class WorkflowInstanceViewer {
  private readonly pluginRegistry: PluginRegistry;
  private readonly eventBus: EventBus;
  private readonly activityNameFormatter: ActivityNameFormatter;
  private readonly journalApi: JournalApi;
  private flowchartElement: HTMLElsaFlowchartElement;
  private container: HTMLDivElement;
  private workflowJournalElement: HTMLElsaWorkflowJournalElement;
  private readonly workflowDefinitionApi: WorkflowDefinitionsApi;
  private readonly workflowDefinitionList: WorkflowDefinition[];

  constructor() {
    this.eventBus = Container.get(EventBus);
    this.pluginRegistry = Container.get(PluginRegistry);
    this.activityNameFormatter = Container.get(ActivityNameFormatter);
    this.journalApi = Container.get(JournalApi);
    this.workflowDefinitionApi = Container.get(WorkflowDefinitionsApi);
    this.workflowDefinitionList = [];
  }

  @Element() private el: HTMLElsaWorkflowDefinitionEditorElement;
  @Prop({attribute: 'monaco-lib-path'}) public monacoLibPath: string;
  @Prop() workflowDefinition: WorkflowDefinition;
  @Prop() workflowInstance: WorkflowInstance;
  @State() private mainWorkflowDefinitionState: WorkflowDefinition;
  @State() private targetWorkflowDefinitionState: WorkflowDefinition;
  @State() private workflowInstanceState: WorkflowInstance;
  @State() private selectedActivity?: Activity;
  @State() private selectedActivityExecutionLog?: WorkflowExecutionLogRecord;

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
    await this.importWorkflow(this.mainWorkflowDefinitionState, this.workflowInstance);
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
    //this.selectedActivity = this.getCurrentContainer();
  }

  @Listen('activitySelected')
  private async handleActivitySelected(e: CustomEvent<ActivitySelectedArgs>) {
    this.selectedActivity = e.detail.activity;
    const workflowInstanceId = this.workflowInstance.id;
    const activityId = this.selectedActivity.id;
    this.selectedActivityExecutionLog = await this.journalApi.getLastEntry({workflowInstanceId, activityId});
  }

  @Listen('journalItemSelected')
  private async handleJournalItemSelected(e: CustomEvent<JournalItemSelectedArgs>) {
    const activityId = e.detail.activity.id;
    const activityNode = e.detail.activityNode;

    let graph = await this.flowchartElement.getGraph();
    let graphNode = graph.getNodes().find(n => n.id == activityId)

    if(graphNode == null) {
      await this.importSelectedItemsWorkflow(activityNode);
      graph.resetSelection();
      this.selectedActivity = e.detail.activity;
    }
    else {
      graph.resetSelection(graphNode);
      this.selectedActivity = graphNode.data;
    }

    var log = e.detail.executionLog;
    this.selectedActivityExecutionLog = log.faulted ? log.faultedRecord : log.completed ? log.completedRecord : log.startedRecord;
  }
  
  private async importSelectedItemsWorkflow(activityNode: ActivityNode) {
    const consumingWorkflowNode = this.findConsumingWorkflowRecursive(activityNode);

    this.targetWorkflowDefinitionState = await this.getWorkflowDefinition(consumingWorkflowNode);

    window.requestAnimationFrame(async () => {
      await this.flowchartElement.updateGraph();
    });
  }

  private findConsumingWorkflowRecursive(activityNode: ActivityNode) : ActivityNode {
    let parent = activityNode.parents[0];
    if(parent == null) {
      return activityNode;
    }
    else{
      var type = parent.activity.type;
      if(type == "Elsa.Workflow" || type == "Elsa.Flowchart") {
        return this.findConsumingWorkflowRecursive(parent);
      }
      else {
        return parent;
      }
    }
  }

  private async getWorkflowDefinition(consumingWorkflowNode: ActivityNode) {
    const isConsumingWorkflowSameAsMain = consumingWorkflowNode.parents[0] == null;
    const activity = consumingWorkflowNode.activity as WorkflowDefinitionActivity;

    return isConsumingWorkflowSameAsMain ? this.workflowDefinition :
      this.workflowDefinitionList.find(w => w.definitionId == activity.workflowDefinitionId) ?? await this.workflowDefinitionApi.get({ definitionId: activity.workflowDefinitionId, versionOptions: { version: activity.version } });
  }

  @Method()
  public async getCanvas(): Promise<HTMLElsaFlowchartElement> {
    return this.flowchartElement;
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
    // Update the flowchart after state is updated.
    window.requestAnimationFrame(async () => {
      await this.flowchartElement.updateGraph();
    });

    await this.eventBus.emit(WorkflowEditorEventTypes.WorkflowDefinition.Imported, this, {workflowDefinition});
  }

  // Updates the workflow definition without importing it into the designer.
  @Method()
  public async updateWorkflowDefinition(workflowDefinition: WorkflowDefinition): Promise<void> {
    this.mainWorkflowDefinitionState = workflowDefinition;
  }

  public async componentWillLoad() {
    this.workflowInstanceState = this.workflowInstance;
    await this.updateWorkflowDefinition(this.workflowDefinition);
  }

  public async componentDidLoad() {
    if (!!this.mainWorkflowDefinitionState && !!this.workflowInstanceState)
      await this.importWorkflow(this.workflowDefinition, this.workflowInstance);

    await this.eventBus.emit(WorkflowEditorEventTypes.WorkflowEditor.Ready, this, {workflowEditor: this});
  }

  private renderSelectedObject = () => {
    const activity = this.selectedActivity;
    if (!!activity)
      return <elsa-activity-properties activity={activity} activityExecutionLog={this.selectedActivityExecutionLog}/>;
  }

  private getWorkflowInternal = async (): Promise<WorkflowDefinition> => {
    const root = await this.flowchartElement.export();
    const workflowDefinition = this.mainWorkflowDefinitionState;
    workflowDefinition.root = root;
    return workflowDefinition;
  };

  private updateLayout = async () => {
    await this.flowchartElement.updateLayout();
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

  public render() {
    const workflowDefinition = this.mainWorkflowDefinitionState;
    const workflowInstance = this.workflowInstanceState;
    return (

      <div class="absolute inset-0" ref={el => this.container = el}>
        <elsa-panel
          class="elsa-activity-picker-container z-30"
          position={PanelPosition.Left}
          onExpandedStateChanged={e => this.onActivityPickerPanelStateChanged(e.detail)}>
          <elsa-workflow-journal
            workflowDefinition={workflowDefinition}
            workflowInstance={workflowInstance}
            ref={el => this.workflowJournalElement = el}
          />
        </elsa-panel>
        <elsa-flowchart
          ref={el => this.flowchartElement = el}
          rootActivity={(this.targetWorkflowDefinitionState ?? this.mainWorkflowDefinitionState).root}
          interactiveMode={false}/>
        <elsa-panel
          class="elsa-workflow-editor-container z-30"
          position={PanelPosition.Right}
          onExpandedStateChanged={e => this.onActivityEditorPanelStateChanged(e.detail)}>
          <div class="object-editor-container">
            <elsa-workflow-instance-properties workflowDefinition={workflowDefinition} workflowInstance={this.workflowInstanceState}/>
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
