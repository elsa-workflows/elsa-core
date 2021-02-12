import {Component, Event, EventEmitter, h, Host, Listen, Prop, State, Watch} from '@stencil/core';
import {ActivityDefinition, ActivityDescriptor, ActivityModel, ConnectionDefinition, ConnectionModel, WorkflowDefinition, WorkflowModel} from "../../../models";
import {createElsaClient, SaveWorkflowDefinitionRequest} from "../../../services/elsa-client";
import state from '../../../utils/store';

@Component({
  tag: 'elsa-workflow-definition-editor',
  styleUrl: 'elsa-workflow-definition-editor.css',
  shadow: false,
})
export class ElsaWorkflowDefinitionEditor {

  constructor() {
    this.workflowDefinition = {
      version: 1,
      connections: [],
      activities: []
    };
  }

  @Prop({attribute: 'workflow-definition-id', reflect: true}) workflowDefinitionId: string;
  @Prop({attribute: 'server-url', reflect: true}) serverUrl: string;
  @State() workflowDefinition: WorkflowDefinition;
  @State() workflowModelInternal: WorkflowModel;
  el: HTMLElement;
  designer: HTMLElsaDesignerTreeElement;

  @Watch('workflowDefinitionId')
  async workflowDefinitionIdChangedHandler(newValue: string) {
    const workflowDefinitionId = newValue;
    let workflowDefinition: WorkflowDefinition = {id: this.workflowDefinitionId, version: 1, activities: [], connections: []};
    const client = createElsaClient(this.serverUrl);

    if (workflowDefinitionId && workflowDefinitionId.length > 0) {
      try {
        workflowDefinition = await client.workflowDefinitionsApi.getByDefinitionAndVersion(workflowDefinitionId, {isLatest: true});
      } catch {
        console.warn(`The specified workflow definition does not exist. Creating a new one.`)
      }
    }

    this.workflowDefinition = workflowDefinition;
    this.workflowModelInternal = this.mapWorkflowModel(workflowDefinition);
  }

  @Listen('workflow-changed')
  async workflowChangedHandler(event: CustomEvent<WorkflowModel>) {
    const workflowModel = event.detail;

    if (this.serverUrl && this.serverUrl.length > 0)
      await this.saveWorkflow(workflowModel);
  }

  async saveWorkflow(workflowModel: WorkflowModel) {
    const client = createElsaClient(this.serverUrl);
    let workflowDefinition = this.workflowDefinition;

    const request: SaveWorkflowDefinitionRequest = {
      workflowDefinitionId: workflowDefinition.id || this.workflowDefinitionId,
      contextOptions: workflowDefinition.contextOptions,
      deleteCompletedInstances: workflowDefinition.deleteCompletedInstances,
      description: workflowDefinition.description,
      displayName: workflowDefinition.displayName,
      enabled: workflowDefinition.isEnabled,
      isSingleton: workflowDefinition.isSingleton,
      name: workflowDefinition.name,
      persistenceBehavior: workflowDefinition.persistenceBehavior,
      publish: workflowDefinition.isPublished,
      variables: workflowDefinition.variables,
      activities: workflowModel.activities.map<ActivityDefinition>(x => ({
        activityId: x.activityId,
        type: x.type,
        name: x.name,
        displayName: x.displayName,
        description: x.description,
        persistWorkflow: false,
        loadWorkflowContext: false,
        saveWorkflowContext: false,
        persistOutput: false,
        properties: x.properties
      })),
      connections: workflowModel.connections.map<ConnectionDefinition>(x => ({
        sourceActivityId: x.sourceId,
        targetActivityId: x.targetId,
        outcome: x.outcome
      })),
    };

    workflowDefinition = await client.workflowDefinitionsApi.save(request);
    this.workflowDefinition = workflowDefinition;
    this.workflowModelInternal = this.mapWorkflowModel(workflowDefinition);
  }

  updateWorkflowDefinition(workflowModel: WorkflowModel) {
    this.workflowDefinition = {
      ...this.workflowDefinition,
      activities: [...workflowModel.activities.map<ActivityDefinition>(x => ({
        activityId: x.activityId,
        type: x.type,
        name: x.name,
        displayName: x.displayName,
        description: x.description,
        persistWorkflow: false,
        loadWorkflowContext: false,
        saveWorkflowContext: false,
        persistOutput: false,
        properties: null
      }))],
      connections: workflowModel.connections.map<ConnectionDefinition>(x => ({
        sourceActivityId: x.sourceId,
        targetActivityId: x.targetId,
        outcome: x.outcome
      }))
    };
  }

  mapWorkflowModel(workflowDefinition: WorkflowDefinition): WorkflowModel {
    return {
      activities: workflowDefinition.activities.map(this.mapActivityModel),
      connections: workflowDefinition.connections.map(this.mapConnectionModel)
    };
  }

  mapActivityModel(source: ActivityDefinition): ActivityModel {
    const descriptors: Array<ActivityDescriptor> = state.activityDescriptors;
    const descriptor = descriptors.find(x => x.type == source.type);

    return {
      activityId: source.activityId,
      description: source.description,
      displayName: source.displayName,
      name: source.name,
      type: source.type,
      properties: source.properties,
      outcomes: [...descriptor.outcomes]
    }
  }

  mapConnectionModel(source: ConnectionDefinition): ConnectionModel {
    return {
      sourceId: source.sourceActivityId,
      targetId: source.targetActivityId,
      outcome: source.outcome
    }
  }

  async componentWillLoad() {
    await this.workflowDefinitionIdChangedHandler(this.workflowDefinitionId);
  }

  componentDidLoad() {
    if (!this.designer) {
      this.designer = this.el.querySelector("elsa-designer-tree") as HTMLElsaDesignerTreeElement;
      this.designer.model = this.workflowModelInternal;
    }
  }

  render() {
    return (
      <Host class="flex flex-col" ref={el => this.el = el}>
        {this.renderContentSlot()}
        {this.renderActivityPicker()}
        {this.renderActivityEditor()}
      </Host>
    );
  }

  renderContentSlot() {
    return (
      <div class="h-screen flex ">
        <elsa-designer-tree model={this.workflowModelInternal} class="flex-1" ref={el => this.designer = el}/>
      </div>
    );
  }

  renderActivityPicker() {
    return <elsa-activity-picker-modal/>;
  }

  renderActivityEditor() {
    return <elsa-activity-editor-modal/>;
  }
}
