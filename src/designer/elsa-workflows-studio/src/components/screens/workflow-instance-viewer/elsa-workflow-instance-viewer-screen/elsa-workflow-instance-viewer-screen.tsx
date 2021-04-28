import {Component, h, Host, Method, Prop, State, Watch} from '@stencil/core';
import {eventBus} from '../../../../services/event-bus';
import * as collection from 'lodash/collection';
import {
  ActivityBlueprint, ActivityDefinitionProperty,
  ActivityDescriptor,
  ActivityModel, Connection,
  ConnectionModel,
  EventTypes, SyntaxNames,
  WorkflowBlueprint, WorkflowExecutionLogRecord,
  WorkflowInstance,
  WorkflowModel,
  WorkflowPersistenceBehavior,
  WorkflowStatus
} from "../../../../models";
import {createElsaClient} from "../../../../services/elsa-client";
import {pluginManager} from '../../../../services/plugin-manager';
import state from '../../../../utils/store';

@Component({
  tag: 'elsa-workflow-instance-viewer-screen',
  shadow: false,
})
export class ElsaWorkflowInstanceViewerScreen {

  constructor() {
    pluginManager.initialize();
  }

  @Prop() workflowInstanceId: string;
  @Prop({attribute: 'server-url', reflect: true}) serverUrl: string;
  @State() workflowInstance: WorkflowInstance;
  @State() workflowBlueprint: WorkflowBlueprint;
  @State() workflowModel: WorkflowModel;
  @State() selectedActivityId?: string;
  el: HTMLElement;
  designer: HTMLElsaDesignerTreeElement;
  journal: HTMLElsaWorkflowInstanceJournalElement;

  @Method()
  async getServerUrl(): Promise<string> {
    return this.serverUrl;
  }

  @Watch('workflowInstanceId')
  async workflowInstanceIdChangedHandler(newValue: string) {
    const workflowInstanceId = newValue;
    let workflowInstance: WorkflowInstance = {
      id: null,
      definitionId: null,
      version: null,
      workflowStatus: WorkflowStatus.Idle,
      variables: {data: {}},
      blockingActivities: [],
      scheduledActivities: [],
      scopes: [],
      currentActivity: {activityId: ''}
    };

    let workflowBlueprint: WorkflowBlueprint = {
      id: null,
      version: 1,
      activities: [],
      connections: [],
      persistenceBehavior: WorkflowPersistenceBehavior.WorkflowBurst,
      customAttributes: {data: {}},
      persistWorkflow: false,
      isLatest: false,
      isPublished: false,
      loadWorkflowContext: false,
      isSingleton: false,
      persistOutput: false,
      saveWorkflowContext: false,
      variables: {data: {}},
      type: null,
      properties: {data: {}}
    };

    const client = createElsaClient(this.serverUrl);

    if (workflowInstanceId && workflowInstanceId.length > 0) {
      try {
        workflowInstance = await client.workflowInstancesApi.get(workflowInstanceId);
        workflowBlueprint = await client.workflowRegistryApi.get(workflowInstance.definitionId, {version: workflowInstance.version});
      } catch {
        console.warn(`The specified workflow definition does not exist. Creating a new one.`);
      }
    }

    this.updateModels(workflowInstance, workflowBlueprint);
  }

  @Watch("serverUrl")
  async serverUrlChangedHandler(newValue: string) {
    if (newValue && newValue.length > 0)
      await this.loadActivityDescriptors();
  }

  async componentWillLoad() {
    await this.serverUrlChangedHandler(this.serverUrl);
    await this.workflowInstanceIdChangedHandler(this.workflowInstanceId);
  }

  componentDidLoad() {
    if (!this.designer) {
      this.designer = this.el.querySelector("elsa-designer-tree") as HTMLElsaDesignerTreeElement;
      this.designer.model = this.workflowModel;
    }
  }

  async loadActivityDescriptors() {
    const client = createElsaClient(this.serverUrl);
    state.activityDescriptors = await client.activitiesApi.list();
  }

  updateModels(workflowInstance: WorkflowInstance, workflowBlueprint: WorkflowBlueprint) {
    this.workflowInstance = workflowInstance;
    this.workflowBlueprint = workflowBlueprint;
    this.workflowModel = this.mapWorkflowModel(workflowBlueprint);
  }

  mapWorkflowModel(workflowBlueprint: WorkflowBlueprint): WorkflowModel {
    return {
      activities: workflowBlueprint.activities.map(this.mapActivityModel),
      connections: workflowBlueprint.connections.map(this.mapConnectionModel),
      persistenceBehavior: workflowBlueprint.persistenceBehavior,
    };
  }

  mapActivityModel(source: ActivityBlueprint): ActivityModel {
    const descriptors: Array<ActivityDescriptor> = state.activityDescriptors;
    const descriptor = descriptors.find(x => x.type == source.type);
    const properties: Array<ActivityDefinitionProperty> = collection.map(source.properties.data, (v, k) => ({name: k, expressions: {'Literal': v}, syntax: SyntaxNames.Literal}));

    return {
      activityId: source.id,
      description: source.description,
      displayName: source.displayName || source.name || source.type,
      name: source.name,
      type: source.type,
      properties: properties,
      outcomes: [...descriptor.outcomes],
      persistOutput: source.persistOutput,
      persistWorkflow: source.persistWorkflow,
      saveWorkflowContext: source.saveWorkflowContext,
      loadWorkflowContext: source.loadWorkflowContext
    }
  }

  mapConnectionModel(connection: Connection): ConnectionModel {
    return {
      sourceId: connection.sourceActivityId,
      targetId: connection.targetActivityId,
      outcome: connection.outcome
    }
  }

  onShowWorkflowSettingsClick() {
    eventBus.emit(EventTypes.ShowWorkflowSettings);
  }

  onRecordSelected(e: CustomEvent<WorkflowExecutionLogRecord>) {
    this.selectedActivityId = e.detail.activityId;
  }

  onActivitySelected(e: CustomEvent<ActivityModel>) {
    this.selectedActivityId = e.detail.activityId;
    this.journal.selectActivityRecord(this.selectedActivityId);
  }

  onActivityDeselected(e: CustomEvent<ActivityModel>) {
    if (this.selectedActivityId == e.detail.activityId)
      this.selectedActivityId = null;

    this.journal.selectActivityRecord(this.selectedActivityId);
  }

  render() {
    const descriptors: Array<ActivityDescriptor> = state.activityDescriptors;
    return (
      <Host class="flex flex-col w-full relative" ref={el => this.el = el}>
        {this.renderCanvas()}
        <elsa-workflow-instance-journal ref={el => this.journal = el}
                                        workflowInstanceId={this.workflowInstanceId}
                                        serverUrl={this.serverUrl}
                                        activityDescriptors={descriptors}
                                        workflowBlueprint={this.workflowBlueprint}
                                        workflowModel={this.workflowModel}
                                        onRecordSelected={e => this.onRecordSelected(e)}/>
      </Host>
    );
  }

  renderCanvas() {
    return (
      <div class="flex-1 flex">
        <elsa-designer-tree model={this.workflowModel}
                            class="flex-1" ref={el => this.designer = el}
                            selectedActivityIds={[this.selectedActivityId]}
                            onActivitySelected={e => this.onActivitySelected(e)}
                            onActivityDeselected={e => this.onActivityDeselected(e)}
        />
      </div>
    );
  }
}
