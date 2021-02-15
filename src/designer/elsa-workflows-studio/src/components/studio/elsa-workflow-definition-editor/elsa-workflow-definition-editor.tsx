import {Component, Event, EventEmitter, h, Host, Listen, Prop, State, Watch} from '@stencil/core';
import {eventBus} from '../../../utils/event-bus';
import {ActivityDefinition, ActivityDescriptor, ActivityModel, ConnectionDefinition, ConnectionModel, EventTypes, WorkflowDefinition, WorkflowModel} from "../../../models";
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
  @State() workflowModel: WorkflowModel;
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

    this.updateWorkflowDefinition(workflowDefinition);
  }

  @Listen('workflow-changed')
  async workflowChangedHandler(event: CustomEvent<WorkflowModel>) {
    const workflowModel = event.detail;
    await this.saveWorkflow(workflowModel);
  }

  async componentWillLoad() {
    await this.workflowDefinitionIdChangedHandler(this.workflowDefinitionId);
  }

  componentDidLoad() {
    if (!this.designer) {
      this.designer = this.el.querySelector("elsa-designer-tree") as HTMLElsaDesignerTreeElement;
      this.designer.model = this.workflowModel;
    }

    eventBus.on(EventTypes.UpdateWorkflowSettings, async (workflowDefinition: WorkflowDefinition) => {
      this.updateWorkflowDefinition(workflowDefinition);
      await this.saveWorkflow(this.workflowModel);
    });
  }

  updateWorkflowDefinition(value: WorkflowDefinition) {
    this.workflowDefinition = value;
    this.workflowModel = this.mapWorkflowModel(value);
  }

  async saveWorkflow(workflowModel: WorkflowModel) {
    if (!this.serverUrl || this.serverUrl.length == 0)
      return;

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
    this.workflowModel = this.mapWorkflowModel(workflowDefinition);
  }

  // updateWorkflowDefinition(workflowModel: WorkflowModel) {
  //   this.workflowDefinition = {
  //     ...this.workflowDefinition,
  //     activities: [...workflowModel.activities.map<ActivityDefinition>(x => ({
  //       activityId: x.activityId,
  //       type: x.type,
  //       name: x.name,
  //       displayName: x.displayName,
  //       description: x.description,
  //       persistWorkflow: false,
  //       loadWorkflowContext: false,
  //       saveWorkflowContext: false,
  //       persistOutput: false,
  //       properties: null
  //     }))],
  //     connections: workflowModel.connections.map<ConnectionDefinition>(x => ({
  //       sourceActivityId: x.sourceId,
  //       targetActivityId: x.targetId,
  //       outcome: x.outcome
  //     }))
  //   };
  // }

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

  onShowWorkflowSettingsClick() {
    eventBus.emit(EventTypes.ShowWorkflowSettings);
  }

  render() {
    return (
      <Host class="flex flex-col" ref={el => this.el = el}>
        {this.renderCanvas()}
        {this.renderActivityPicker()}
        {this.renderActivityEditor()}
      </Host>
    );
  }

  renderCanvas() {
    return (
      <div class="h-screen flex relative">
        <elsa-designer-tree model={this.workflowModel} class="flex-1" ref={el => this.designer = el}/>
        {this.renderDefinitionPropertiesButton()}
        {this.renderWorkflowSettingsModal()}
      </div>
    );
  }

  renderActivityPicker() {
    return <elsa-activity-picker-modal/>;
  }

  renderActivityEditor() {
    return <elsa-activity-editor-modal/>;
  }

  renderDefinitionPropertiesButton() {
    return (
      <button onClick={() => this.onShowWorkflowSettingsClick()} type="button"
              class="fixed top-10 right-12 inline-flex items-center p-2 rounded-full border border-transparent bg-white shadow text-gray-400 hover:text-blue-500 focus:text-blue-500 hover:ring-2 hover:ring-offset-2 hover:ring-blue-500 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500">
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" stroke="currentColor" fill="none" class="h-8 w-8">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z"/>
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"/>
        </svg>
      </button>
    );
  }

  renderWorkflowSettingsModal() {
    return <elsa-workflow-definition-settings-modal workflowDefinition={this.workflowDefinition}/>;
  }
}
