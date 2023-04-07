import {Component, h, Listen, Prop, State, Method, Watch, Element} from '@stencil/core';
import {Container} from "typedi";
import {PanelPosition, PanelStateChangedArgs} from '../../../components/panel/models';
import {
  Activity,
  ActivitySelectedArgs,
  ContainerSelectedArgs,
  GraphUpdatedArgs,
  WorkflowInstance,
  WorkflowExecutionLogRecord
} from '../../../models';
import {PluginRegistry, ActivityNameFormatter, ActivityDriverRegistry, EventBus} from '../../../services';
import {MonacoEditorSettings} from "../../../services/monaco-editor-settings";
import {WorkflowDefinition} from "../../workflow-definitions/models/entities";
import {WorkflowEditorEventTypes} from "../../workflow-definitions/models/ui";
import { JournalItemSelectedArgs } from '../events';
import {JournalApi} from "../services/journal-api";

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

  constructor() {
    this.eventBus = Container.get(EventBus);
    this.pluginRegistry = Container.get(PluginRegistry);
    this.activityNameFormatter = Container.get(ActivityNameFormatter);
    this.journalApi = Container.get(JournalApi);
  }

  @Element() private el: HTMLElsaWorkflowDefinitionEditorElement;
  @Prop({attribute: 'monaco-lib-path'}) public monacoLibPath: string;
  @Prop() workflowDefinition: WorkflowDefinition;
  @Prop() workflowInstance: WorkflowInstance;
  @State() private workflowDefinitionState: WorkflowDefinition;
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
    const graph = await this.flowchartElement.getGraph();
    const node = graph.getNodes().find(n => n.id == activityId)

    if (node != null) {
      graph.resetSelection(node);
      this.selectedActivity = node.data;
    }
    else {
      graph.resetSelection();
      this.selectedActivity = e.detail.activity;
    }

    var log = e.detail.executionLog;
    this.selectedActivityExecutionLog = log.faulted ? log.faultedRecord : log.completed ? log.completedRecord : log.startedRecord;
  }

  @Listen('graphUpdated')
  private handleGraphUpdated(e: CustomEvent<GraphUpdatedArgs>) {
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
    this.workflowDefinitionState = workflowDefinition;
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

  private renderSelectedObject = () => {
    const activity = this.selectedActivity;
    if (!!activity)
      return <elsa-activity-properties activity={activity} activityExecutionLog={this.selectedActivityExecutionLog}/>;
  }

  private getWorkflowInternal = async (): Promise<WorkflowDefinition> => {
    const root = await this.flowchartElement.export();
    const workflowDefinition = this.workflowDefinitionState;
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
    const workflowDefinition = this.workflowDefinitionState;
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
          rootActivity={workflowDefinition.root}
          interactiveMode={false}/>
        <elsa-panel
          class="elsa-workflow-editor-container z-30"
          position={PanelPosition.Right}
          onExpandedStateChanged={e => this.onActivityEditorPanelStateChanged(e.detail)}>
          <div class="object-editor-container">
            <elsa-workflow-instance-properties workflowDefinition={this.workflowDefinitionState} workflowInstance={this.workflowInstanceState}/>
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
