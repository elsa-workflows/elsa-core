import {Component, h, Host, Method, Prop, State, Watch} from '@stencil/core';
import * as collection from 'lodash/collection';
import {
  ActivityBlueprint, ActivityDefinitionProperty,
  ActivityDescriptor,
  ActivityModel, Connection,
  ConnectionModel,
  SyntaxNames,
  WorkflowBlueprint, WorkflowModel,
  WorkflowPersistenceBehavior
} from "../../../../models";
import {createElsaClient} from "../../../../services";
import state from '../../../../utils/store';
import {WorkflowDesignerMode} from "../../../designers/tree/elsa-designer-tree/models";
import Tunnel from "../../../../data/dashboard";

@Component({
  tag: 'elsa-workflow-blueprint-viewer-screen',
  shadow: false,
})
export class ElsaWorkflowBlueprintViewerScreen {

  @Prop() workflowDefinitionId: string;
  @Prop() serverUrl: string;
  @Prop() culture: string;
  @State() workflowBlueprint: WorkflowBlueprint;
  @State() workflowModel: WorkflowModel;
  el: HTMLElement;
  designer: HTMLElsaDesignerTreeElement;

  @Method()
  async getServerUrl(): Promise<string> {
    return this.serverUrl;
  }

  @Watch('workflowDefinitionId')
  async workflowDefinitionIdChangedHandler(newValue: string) {
    const workflowDefinitionId = newValue;
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
      saveWorkflowContext: false,
      variables: {data: {}},
      type: null,
      inputProperties: {data: {}},
      outputProperties: {data: {}},
      propertyStorageProviders: {}
    };

    const client = await createElsaClient(this.serverUrl);

    if (workflowDefinitionId && workflowDefinitionId.length > 0) {
      try {
        workflowBlueprint = await client.workflowRegistryApi.get(workflowDefinitionId, {isLatest: true});
      } catch {
        console.warn(`The specified workflow blueprint does not exist. Creating a new one.`);
      }
    }

    this.updateModels(workflowBlueprint);
  }

  @Watch("serverUrl")
  async serverUrlChangedHandler(newValue: string) {
    if (newValue && newValue.length > 0)
      await this.loadActivityDescriptors();
  }

  async componentWillLoad() {
    await this.serverUrlChangedHandler(this.serverUrl);
    await this.workflowDefinitionIdChangedHandler(this.workflowDefinitionId);
  }

  componentDidLoad() {
    if (!this.designer) {
      if (state.useX6Graphs) {
        this.designer = this.el.querySelector("x6-designer") as HTMLX6DesignerElement;
      } else {
        this.designer = this.el.querySelector('elsa-designer-tree') as HTMLElsaDesignerTreeElement;
      }
      this.designer.model = this.workflowModel;
    }
  }

  async loadActivityDescriptors() {
    const client = await createElsaClient(this.serverUrl);
    state.activityDescriptors = await client.activitiesApi.list();
  }

  updateModels(workflowBlueprint: WorkflowBlueprint) {
    this.workflowBlueprint = workflowBlueprint;
    this.workflowModel = this.mapWorkflowModel(workflowBlueprint);
  }

  mapWorkflowModel(workflowBlueprint: WorkflowBlueprint): WorkflowModel {
    return {
      activities: workflowBlueprint.activities.filter(x => x.parentId == workflowBlueprint.id || !x.parentId).map(this.mapActivityModel),
      connections: workflowBlueprint.connections.map(this.mapConnectionModel),
      persistenceBehavior: workflowBlueprint.persistenceBehavior,
    };
  }

  mapActivityModel(source: ActivityBlueprint): ActivityModel {
    const activityDescriptors: Array<ActivityDescriptor> = state.activityDescriptors;
    const activityDescriptor = activityDescriptors.find(x => x.type == source.type);
    const properties: Array<ActivityDefinitionProperty> = collection.map(source.inputProperties.data, (value, key) => {
      const propertyDescriptor = activityDescriptor.inputProperties.find(x => x.name == key);
      const defaultSyntax = propertyDescriptor?.defaultSyntax || SyntaxNames.Literal;
      const expressions = {};
      expressions[defaultSyntax] = value;
      return ({name: key, expressions: expressions, syntax: defaultSyntax});
    });

    return {
      activityId: source.id,
      description: source.description,
      displayName: source.displayName || source.name || source.type,
      name: source.name,
      type: source.type,
      x: source.x,
      y: source.y,
      properties: properties,
      outcomes: [...activityDescriptor.outcomes],
      persistWorkflow: source.persistWorkflow,
      saveWorkflowContext: source.saveWorkflowContext,
      loadWorkflowContext: source.loadWorkflowContext,
      propertyStorageProviders: source.propertyStorageProviders,
    }
  }

  mapConnectionModel(connection: Connection): ConnectionModel {
    return {
      sourceId: connection.sourceActivityId,
      targetId: connection.targetActivityId,
      outcome: connection.outcome
    }
  }

  render() {
    return (
      <Host class="elsa-flex elsa-flex-col elsa-w-full elsa-relative" ref={el => this.el = el}>
        {this.renderCanvas()}
      </Host>
    );
  }

  renderCanvas() {
    return (
      <div class="elsa-flex-1 elsa-flex">
        {!state.useX6Graphs && (
          <elsa-designer-tree
            model={this.workflowModel}
            class="elsa-flex-1"
            ref={el => this.designer = el}
            mode={WorkflowDesignerMode.Blueprint}
          />
        )}
        {state.useX6Graphs && (
          <x6-designer
            model={this.workflowModel}
            class="elsa-workflow-wrapper"
            ref={el => this.designer = el}
            mode={WorkflowDesignerMode.Blueprint}
          />
        )}
        <elsa-flyout-panel>
          <elsa-tab-header tab="general" slot="header">General</elsa-tab-header>
          <elsa-tab-content tab="general" slot="content">
            <elsa-workflow-blueprint-properties-panel workflowId={this.workflowDefinitionId}/>
          </elsa-tab-content>
        </elsa-flyout-panel>
      </div>
    );
  }
}

Tunnel.injectProps(ElsaWorkflowBlueprintViewerScreen, ['serverUrl', 'culture']);
