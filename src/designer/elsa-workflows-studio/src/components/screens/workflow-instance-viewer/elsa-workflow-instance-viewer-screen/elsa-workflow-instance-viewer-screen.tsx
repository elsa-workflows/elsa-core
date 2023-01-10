import {Component, h, Host, Listen, Method, Prop, State, Watch} from '@stencil/core';
import {eventBus} from '../../../../services/event-bus';
import * as collection from 'lodash/collection';
import {
  ActivityBlueprint,
  ActivityDefinitionProperty,
  ActivityDescriptor,
  ActivityModel,
  Connection,
  ConnectionModel,
  EventTypes,
  SyntaxNames,
  WorkflowBlueprint,
  WorkflowExecutionLogRecord,
  WorkflowInstance,
  WorkflowModel,
  WorkflowPersistenceBehavior,
  WorkflowStatus
} from "../../../../models";
import {ActivityStats, createElsaClient} from "../../../../services/elsa-client";
import state from '../../../../utils/store';
import {
  ActivityContextMenuState,
  LayoutDirection,
  WorkflowDesignerMode
} from "../../../designers/tree/elsa-designer-tree/models";
import Tunnel from "../../../../data/dashboard";
import {featuresDataManager} from "../../../../services";

@Component({
  tag: 'elsa-workflow-instance-viewer-screen',
  shadow: false,
})
export class ElsaWorkflowInstanceViewerScreen {

  @Prop({attribute:'workflow-instance-id'}) workflowInstanceId: string;
  @Prop({attribute: 'server-url'}) serverUrl: string;
  @Prop() culture: string;
  @State() workflowInstance: WorkflowInstance;
  @State() workflowBlueprint: WorkflowBlueprint;
  @State() workflowModel: WorkflowModel;
  @State() selectedActivityId?: string;
  @State() activityStats?: ActivityStats;

  @State() activityContextMenuState: ActivityContextMenuState = {
    shown: false,
    x: 0,
    y: 0,
    activity: null,
  };

  el: HTMLElement;
  designer: HTMLElsaDesignerTreeElement;
  journal: HTMLElsaWorkflowInstanceJournalElement;
  contextMenu: HTMLElement;
  layoutDirection = LayoutDirection.TopBottom;

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
      definitionVersionId: null,
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
      saveWorkflowContext: false,
      variables: {data: {}},
      type: null,
      inputProperties: {data: {}},
      outputProperties: {data: {}},
      propertyStorageProviders: {}
    };

    const client = await createElsaClient(this.serverUrl);

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

  @Listen('click', {target: 'window'})
  onWindowClicked(event: Event){
    const target = event.target as HTMLElement;

    if (!this.contextMenu.contains(target))
      this.handleContextMenuChange(0, 0, false, null);
  }

  async componentWillLoad() {
    const layoutFeature = featuresDataManager.getFeatureConfig(featuresDataManager.supportedFeatures.workflowLayout);

    if (layoutFeature && layoutFeature.enabled) {
      this.layoutDirection = layoutFeature.value as LayoutDirection;
    }

    await this.serverUrlChangedHandler(this.serverUrl);
    await this.workflowInstanceIdChangedHandler(this.workflowInstanceId);
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

  componentDidRender() {
    if (this.el && this.contextMenu) {
      let modalX = this.activityContextMenuState.x + 64;
      let modalY = this.activityContextMenuState.y - 256;

      // Fit the modal to the canvas bounds
      const canvasBounds = this.el?.getBoundingClientRect();
      const modalBounds = this.contextMenu.getBoundingClientRect();
      const modalWidth = modalBounds?.width;
      const modalHeight = modalBounds?.height;
      modalX = Math.min(canvasBounds.width, modalX + modalWidth + 32) - modalWidth - 32;
      modalY = Math.min(canvasBounds.height, modalY + modalHeight) - modalHeight - 32;
      modalY = Math.max(0, modalY);

      this.contextMenu.style.left = `${modalX}px`;
      this.contextMenu.style.top = `${modalY}px`;
    }
  }

  async loadActivityDescriptors() {
    const client = await createElsaClient(this.serverUrl);
    state.activityDescriptors = await client.activitiesApi.list();
  }

  updateModels(workflowInstance: WorkflowInstance, workflowBlueprint: WorkflowBlueprint) {
    this.workflowInstance = workflowInstance;
    this.workflowBlueprint = workflowBlueprint;
    this.workflowModel = this.mapWorkflowModel(workflowBlueprint, workflowInstance);
  }

  mapWorkflowModel(workflowBlueprint: WorkflowBlueprint, workflowInstance: WorkflowInstance): WorkflowModel {
    const activities = workflowBlueprint.activities.filter(x => x.parentId == workflowBlueprint.id || !x.parentId).map(x => this.mapActivityModel(x, workflowInstance));
    const connections = workflowBlueprint.connections.filter(c => activities.findIndex(a => a.activityId == c.sourceActivityId || a.activityId == c.targetActivityId) > -1).map(this.mapConnectionModel);

    return {
      activities: activities,
      connections: connections,
      persistenceBehavior: workflowBlueprint.persistenceBehavior,
    };
  }

  mapActivityModel(activityBlueprint: ActivityBlueprint, workflowInstance: WorkflowInstance): ActivityModel {
    const activityDescriptors: Array<ActivityDescriptor> = state.activityDescriptors;
    const activityDescriptor = activityDescriptors.find(x => x.type == activityBlueprint.type);
    const activityData = workflowInstance.activityData[activityBlueprint.id] || {};

    const properties: Array<ActivityDefinitionProperty> = collection.map(activityBlueprint.inputProperties.data, (value, key) => {
      const propertyDescriptor = activityDescriptor.inputProperties.find(x => x.name == key) || activityDescriptor.outputProperties.find(x => x.name == key);
      const defaultSyntax = propertyDescriptor?.defaultSyntax || SyntaxNames.Literal;
      const expressions = {};
      const v = activityData[key] || value;
      expressions[defaultSyntax] = v;
      return ({name: key, value: v, expressions: expressions, syntax: defaultSyntax});
    });

    return {
      activityId: activityBlueprint.id,
      description: activityBlueprint.description,
      displayName: activityBlueprint.displayName || activityBlueprint.name || activityBlueprint.type,
      name: activityBlueprint.name,
      type: activityBlueprint.type,
      properties: properties,
      outcomes: [...activityDescriptor.outcomes],
      persistWorkflow: activityBlueprint.persistWorkflow,
      saveWorkflowContext: activityBlueprint.saveWorkflowContext,
      loadWorkflowContext: activityBlueprint.loadWorkflowContext,
      propertyStorageProviders: activityBlueprint.propertyStorageProviders,
      x: activityBlueprint.x,
      y: activityBlueprint.y,
    }
  }

  mapConnectionModel(connection: Connection): ConnectionModel {
    return {
      sourceId: connection.sourceActivityId,
      targetId: connection.targetActivityId,
      outcome: connection.outcome
    }
  }

  handleContextMenuChange(x: number, y: number, shown: boolean, activity: ActivityModel) {
    this.activityContextMenuState = {
      shown,
      x,
      y,
      activity,
    };
  }

  onShowWorkflowSettingsClick() {
    eventBus.emit(EventTypes.ShowWorkflowSettings);
  }

  onRecordSelected(e: CustomEvent<WorkflowExecutionLogRecord>) {
    const record = e.detail;
    const activity = !!record ? this.workflowBlueprint.activities.find(x => x.id === record.activityId) : null;
    this.selectedActivityId = activity != null ? activity.parentId != null ? activity.parentId : activity.id : null;
  }

  async onActivitySelected(e: CustomEvent<ActivityModel>) {
    this.selectedActivityId = e.detail.activityId;
    await this.journal.selectActivityRecord(this.selectedActivityId);
  }

  async onActivityDeselected(e: CustomEvent<ActivityModel>) {
    if (this.selectedActivityId == e.detail.activityId)
      this.selectedActivityId = null;

    await this.journal.selectActivityRecord(this.selectedActivityId);
  }

  async onActivityContextMenuButtonClicked(e: CustomEvent<ActivityContextMenuState>) {
    this.activityContextMenuState = e.detail;
    this.activityStats = null;

    if (!e.detail.shown) {
      return;
    }

    const elsaClient = await createElsaClient(this.serverUrl);
    this.activityStats = await elsaClient.activityStatsApi.get(this.workflowInstanceId, e.detail.activity.activityId);
  }

  render() {
    const descriptors: Array<ActivityDescriptor> = state.activityDescriptors;
    return (
      <Host class="elsa-flex elsa-flex-col elsa-w-full elsa-relative" ref={el => this.el = el}>
        {this.renderCanvas()}
        <elsa-workflow-instance-journal ref={el => this.journal = el}
                                        workflowInstanceId={this.workflowInstanceId}
                                        workflowInstance={this.workflowInstance}
                                        serverUrl={this.serverUrl}
                                        activityDescriptors={descriptors}
                                        workflowBlueprint={this.workflowBlueprint}
                                        workflowModel={this.workflowModel}
                                        onRecordSelected={e => this.onRecordSelected(e)}/>
      </Host>
    );
  }

  getActivityBorderColor = (activity: ActivityModel): string => {
    const workflowInstance = this.workflowInstance;
    const workflowFault = !!workflowInstance ? workflowInstance.faults : null;
    const activityData = workflowInstance.activityData[activity.activityId] || {};
    const lifecycle = activityData['_Lifecycle'] || {};
    const executing = lifecycle.executing ?? lifecycle.Executing;
    const executed = lifecycle.executed ?? lifecycle.Executed;

    if (!!workflowFault && workflowFault.find(x => x.faultedActivityId == activity.activityId))
      return 'red';

    if (executed)
      return 'green';

    if (executing)
      return 'blue';

    return null;
  }

  renderActivityStatsButton = (activity: ActivityModel): string => {
    const workflowInstance = this.workflowInstance;
    const workflowFault = !!workflowInstance ? workflowInstance.faults : null;
    const activityData = workflowInstance.activityData[activity.activityId] || {};
    const lifecycle = activityData['_Lifecycle'] || {};
    const executing = lifecycle.executing ?? lifecycle.Executing;
    const executed = lifecycle.executed ?? lifecycle.Executed;

    let icon: string;

    if (!!workflowFault && workflowFault.find(x => x.faultedActivityId == activity.activityId)) {
      icon = `<svg class="elsa-flex-shrink-0 elsa-h-6 elsa-w-6 elsa-text-red-600" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <circle cx="12" cy="12" r="10"/>
                <line x1="12" y1="8" x2="12" y2="12"/>
                <line x1="12" y1="16" x2="12.01" y2="16"/>
              </svg>`;
    } else if (executed) {
      icon = `<svg class="elsa-h-6 elsa-w-6 elsa-text-green-500"  viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <circle cx="12" cy="12" r="10" />
                <line x1="12" y1="16" x2="12" y2="12" />
                <line x1="12" y1="8" x2="12.01" y2="8" />
              </svg>`;
    } else if (executing) {
      icon = `<svg class="elsa-h-6 elsa-w-6 elsa-text-blue-500" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <circle cx="12" cy="12" r="10" />
                <line x1="12" y1="16" x2="12" y2="12" />
                <line x1="12" y1="8" x2="12.01" y2="8" />
              </svg>`;
    } else {
      icon = `<svg class="h-6 w-6 text-gray-300" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <circle cx="12" cy="12" r="10" />
                <line x1="12" y1="16" x2="12" y2="12" />
                <line x1="12" y1="8" x2="12.01" y2="8" />
              </svg>`
    }

    return `<div class="context-menu-wrapper elsa-flex-shrink-0">
            <button aria-haspopup="true"
                    class="elsa-w-8 elsa-h-8 elsa-inline-flex elsa-items-center elsa-justify-center elsa-text-gray-400 elsa-rounded-full elsa-bg-transparent hover:elsa-text-gray-500 focus:elsa-outline-none focus:elsa-text-gray-500 focus:elsa-bg-gray-100 elsa-transition elsa-ease-in-out elsa-duration-150">
              ${icon}
            </button>
          </div>`;
  }

  renderCanvas() {
    return (
      <div class="elsa-flex-1 elsa-flex">
        {!state.useX6Graphs && (
          <elsa-designer-tree
            model={this.workflowModel}
            class="elsa-flex-1"
            ref={el => (this.designer = el)}
            layoutDirection={this.layoutDirection}
            mode={WorkflowDesignerMode.Instance}
            activityContextMenuButton={this.renderActivityStatsButton}
            activityBorderColor={this.getActivityBorderColor}
            activityContextMenu={this.activityContextMenuState}
            selectedActivityIds={[this.selectedActivityId]}
            onActivitySelected={e => this.onActivitySelected(e)}
            onActivityDeselected={e => this.onActivityDeselected(e)}
            onActivityContextMenuButtonClicked={e => this.onActivityContextMenuButtonClicked(e)}
          />
        )}
        {state.useX6Graphs && (
          <x6-designer
            model={this.workflowModel}
            class="elsa-workflow-wrapper"
            ref={el => (this.designer = el)}
            layoutDirection={this.layoutDirection}
            mode={WorkflowDesignerMode.Instance}
            activityContextMenuButton={this.renderActivityStatsButton}
            activityBorderColor={this.getActivityBorderColor}
            activityContextMenu={this.activityContextMenuState}
            selectedActivityIds={[this.selectedActivityId]}
            onActivitySelected={e => this.onActivitySelected(e)}
            onActivityDeselected={e => this.onActivityDeselected(e)}
            onActivityContextMenuButtonClicked={e => this.onActivityContextMenuButtonClicked(e)}
          />
        )}
        {this.renderActivityPerformanceMenu()}
      </div>
    );
  }

  renderActivityPerformanceMenu = () => {
    const activityStats: ActivityStats = this.activityStats;

    const renderFault = () => {
      if (!activityStats.fault)
        return;

      return <elsa-workflow-fault-information workflowFault={this.workflowInstance.faults.find(x => x.faultedActivityId == this.selectedActivityId)} faultedAt={this.workflowInstance.faultedAt} />;
    };

    const renderPerformance = () => {
      if (!!activityStats.fault) return;

      return <elsa-workflow-performance-information activityStats={activityStats} />;
    }

    const renderStats = function () {
      return (
        <div>
          <div>
            <table class="elsa-min-w-full elsa-divide-y elsa-divide-gray-200 elsa-border-b elsa-border-gray-200">
              <thead class="elsa-bg-gray-50">
              <tr>
                <th scope="col" class="elsa-px-6 elsa-py-3 elsa-text-left elsa-text-xs elsa-font-medium elsa-text-gray-500 elsa-text-right elsa-tracking-wider">
                  Event
                </th>
                <th scope="col" class="elsa-px-6 elsa-py-3 elsa-text-left elsa-text-xs elsa-font-medium elsa-text-gray-500 elsa-text-right elsa-tracking-wider">
                  Count
                </th>
              </tr>
              </thead>
              <tbody class="elsa-bg-white elsa-divide-y elsa-divide-gray-200">
              {activityStats.eventCounts.length > 0 ? activityStats.eventCounts.map(eventCount => (
                <tr>
                  <td class="elsa-px-6 elsa-py-4 elsa-whitespace-nowrap elsa-text-sm elsa-font-medium elsa-text-gray-900">
                    {eventCount.eventName}
                  </td>
                  <td class="elsa-px-6 elsa-py-4 elsa-whitespace-nowrap elsa-text-sm elsa-text-gray-500">
                    {eventCount.count}
                  </td>
                </tr>)) : <tr>
                <td colSpan={2} class="elsa-px-6 elsa-py-4 elsa-whitespace-nowrap elsa-text-sm elsa-font-medium elsa-text-gray-900">
                  No events record for this activity.
                </td>
              </tr>}
              </tbody>
            </table>
          </div>

          {activityStats.eventCounts.length > 0 ? (<div class="elsa-relative elsa-grid elsa-gap-6 elsa-bg-white px-5 elsa-py-6 sm:elsa-gap-8 sm:elsa-p-8">
            {renderFault()}
            {renderPerformance()}
          </div>) : undefined
          }
        </div>
      )
    };

    const renderLoader = function () {
      return <div class="elsa-p-6 elsa-bg-white">Loading...</div>;
    };

    return <div
      data-transition-enter="elsa-transition elsa-ease-out elsa-duration-100"
      data-transition-enter-start="elsa-transform elsa-opacity-0 elsa-scale-95"
      data-transition-enter-end="elsa-transform elsa-opacity-100 elsa-scale-100"
      data-transition-leave="elsa-transition elsa-ease-in elsa-duration-75"
      data-transition-leave-start="elsa-transform elsa-opacity-100 elsa-scale-100"
      data-transition-leave-end="elsa-transform elsa-opacity-0 elsa-scale-95"
      class={`${this.activityContextMenuState.shown ? '' : 'hidden'} elsa-absolute elsa-z-10 elsa-mt-3 elsa-px-2 elsa-w-screen elsa-max-w-xl sm:elsa-px-0`}
      style={{left: `${this.activityContextMenuState.x + 64}px`, top: `${this.activityContextMenuState.y - 256}px`}}
      ref={el => this.contextMenu = el}
    >
      <div class="elsa-rounded-lg elsa-shadow-lg elsa-ring-1 elsa-ring-black elsa-ring-opacity-5 elsa-overflow-hidden">
        {!!activityStats ? renderStats() : renderLoader()}
      </div>
    </div>
  }
}
Tunnel.injectProps(ElsaWorkflowInstanceViewerScreen, ['serverUrl', 'culture']);
