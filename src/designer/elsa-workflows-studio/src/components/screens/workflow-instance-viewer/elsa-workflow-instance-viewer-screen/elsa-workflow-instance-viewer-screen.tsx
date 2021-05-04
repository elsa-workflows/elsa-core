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
import {ActivityStats, createElsaClient} from "../../../../services/elsa-client";
import {pluginManager} from '../../../../services/plugin-manager';
import state from '../../../../utils/store';
import {ActivityContextMenuState, WorkflowDesignerMode} from "../../../designers/tree/elsa-designer-tree/models";
import {registerClickOutside} from "stencil-click-outside";
import {editor} from "monaco-editor";
import create = editor.create;
import moment from "moment";
import {durationToString} from "../../../../utils/utils";

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
      activities: workflowBlueprint.activities.filter(x => x.parentId == workflowBlueprint.id || !x.parentId).map(this.mapActivityModel),
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

    const elsaClient = createElsaClient(this.serverUrl);
    this.activityStats = await elsaClient.activityStatsApi.get(this.workflowInstanceId, e.detail.activity.activityId);
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
    const activityStatsButton =
      `<div class="context-menu-wrapper flex-shrink-0">
            <button aria-haspopup="true"
                    class="w-8 h-8 inline-flex items-center justify-center text-gray-400 rounded-full bg-transparent hover:text-gray-500 focus:outline-none focus:text-gray-500 focus:bg-gray-100 transition ease-in-out duration-150">
              <svg class="h-6 w-6 text-blue-600" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <circle cx="12" cy="12" r="10" /> 
                <line x1="12" y1="16" x2="12" y2="12" />
                <line x1="12" y1="8" x2="12.01" y2="8" />
              </svg>
            </button>
          </div>`;

    return (
      <div class="flex-1 flex">
        <elsa-designer-tree model={this.workflowModel}
                            class="flex-1" ref={el => this.designer = el}
                            mode={WorkflowDesignerMode.Instance}
                            activityContextMenuButton={activityStatsButton}
                            activityContextMenu={this.activityContextMenuState}
                            selectedActivityIds={[this.selectedActivityId]}
                            onActivitySelected={e => this.onActivitySelected(e)}
                            onActivityDeselected={e => this.onActivityDeselected(e)}
                            onActivityContextMenuButtonClicked={e => this.onActivityContextMenuButtonClicked(e)}
        />
        {this.renderActivityPerformanceMenu()}
      </div>
    );
  }

  renderActivityPerformanceMenu() {
    const activityStats: ActivityStats = this.activityStats;

    const renderStats = function () {
      return (
        <div>
          <div>
            <table class="min-w-full divide-y divide-gray-200 border-b border-gray-200">
              <thead class="bg-gray-50">
              <tr>
                <th scope="col" class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Event
                </th>
                <th scope="col" class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Count
                </th>
              </tr>
              </thead>
              <tbody class="bg-white divide-y divide-gray-200">
              {activityStats.eventCounts.map(eventCount => (
                <tr>
                  <td class="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                    {eventCount.eventName}
                  </td>
                  <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                    {eventCount.count}
                  </td>
                </tr>))}
              </tbody>
            </table>
          </div>

          <div class="relative grid gap-6 bg-white px-5 py-6 sm:gap-8 sm:p-8">

            {!!activityStats.fault ? (
              <a href="#" class="-m-3 p-3 flex items-start rounded-lg hover:bg-gray-50 transition ease-in-out duration-150">
                <svg class="flex-shrink-0 h-6 w-6 text-red-600" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                  <circle cx="12" cy="12" r="10"/>
                  <line x1="12" y1="8" x2="12" y2="12"/>
                  <line x1="12" y1="16" x2="12.01" y2="16"/>
                </svg>
                <div class="ml-4">
                  <p class="text-base font-medium text-gray-900">
                    Fault
                  </p>
                  <p class="mt-1 text-sm text-gray-500">
                    {activityStats.fault.message}
                  </p>
                </div>
              </a>) : undefined}

            <a href="#" class="-m-3 p-3 flex items-start rounded-lg hover:bg-gray-50 transition ease-in-out duration-150">
              <svg class="flex-shrink-0 h-6 w-6 text-indigo-500" width="24" height="24" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor" fill="none" stroke-linecap="round" stroke-linejoin="round">
                <path stroke="none" d="M0 0h24v24H0z"/>
                <circle cx="12" cy="12" r="9"/>
                <polyline points="12 7 12 12 15 15"/>
              </svg>
              <div class="ml-4">
                <p class="text-base font-medium text-gray-900">
                  Average Execution Time
                </p>
                <p class="mt-1 text-sm text-gray-500">
                  {durationToString(moment.duration(activityStats.averageExecutionTime))}
                </p>
              </div>
            </a>

            <a href="#" class="-m-3 p-3 flex items-start rounded-lg hover:bg-gray-50 transition ease-in-out duration-150">
              <svg class="flex-shrink-0 h-6 w-6 text-green-500" width="24" height="24" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor" fill="none" stroke-linecap="round" stroke-linejoin="round">
                <path stroke="none" d="M0 0h24v24H0z"/>
                <circle cx="12" cy="12" r="9"/>
                <polyline points="12 7 12 12 15 15"/>
              </svg>
              <div class="ml-4">
                <p class="text-base font-medium text-gray-900">
                  Fastest Execution Time
                </p>
                <p class="mt-1 text-sm text-gray-500">
                  {durationToString(moment.duration(activityStats.fastestExecutionTime))}
                </p>
              </div>
            </a>

            <a href="#" class="-m-3 p-3 flex items-start rounded-lg hover:bg-gray-50 transition ease-in-out duration-150">
              <svg class="flex-shrink-0 h-6 w-6 text-yellow-500" width="24" height="24" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor" fill="none" stroke-linecap="round" stroke-linejoin="round">
                <path stroke="none" d="M0 0h24v24H0z"/>
                <circle cx="12" cy="12" r="9"/>
                <polyline points="12 7 12 12 15 15"/>
              </svg>
              <div class="ml-4">
                <p class="text-base font-medium text-gray-900">
                  Slowest Execution Time
                </p>
                <p class="mt-1 text-sm text-gray-500">
                  {durationToString(moment.duration(activityStats.slowestExecutionTime))}
                </p>
              </div>
            </a>

            <a href="#" class="-m-3 p-3 flex items-start rounded-lg hover:bg-gray-50 transition ease-in-out duration-150">
              <svg class="flex-shrink-0 h-6 w-6 text-blue-600" width="24" height="24" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor" fill="none" stroke-linecap="round" stroke-linejoin="round">
                <path stroke="none" d="M0 0h24v24H0z"/>
                <rect x="4" y="5" width="16" height="16" rx="2"/>
                <line x1="16" y1="3" x2="16" y2="7"/>
                <line x1="8" y1="3" x2="8" y2="7"/>
                <line x1="4" y1="11" x2="20" y2="11"/>
                <line x1="11" y1="15" x2="12" y2="15"/>
                <line x1="12" y1="15" x2="12" y2="18"/>
              </svg>
              <div class="ml-4">
                <p class="text-base font-medium text-gray-900">
                  Last Executed At
                </p>
                <p class="mt-1 text-sm text-gray-500">
                  {moment(activityStats.lastExecutedAt).format('DD-MM-YYYY HH:mm:ss')}
                </p>
              </div>
            </a>
          </div>
        </div>
      )
    };

    const renderLoader = function () {
      return <div>Loading...</div>;
    };

    return <div
      data-transition-enter="transition ease-out duration-100"
      data-transition-enter-start="transform opacity-0 scale-95"
      data-transition-enter-end="transform opacity-100 scale-100"
      data-transition-leave="transition ease-in duration-75"
      data-transition-leave-start="transform opacity-100 scale-100"
      data-transition-leave-end="transform opacity-0 scale-95"
      class={`${this.activityContextMenuState.shown ? '' : 'hidden'} absolute z-10 mt-3 px-2 w-screen max-w-xl sm:px-0`}
      style={{left: `${this.activityContextMenuState.x + 64}px`, top: `${this.activityContextMenuState.y - 256}px`}}
      ref={el =>
        registerClickOutside(this, el, () => {
          this.handleContextMenuChange(0, 0, false, null);
        })
      }
    >
      <div class="rounded-lg shadow-lg ring-1 ring-black ring-opacity-5 overflow-hidden">
        {!!activityStats ? renderStats() : renderLoader()}

      </div>
    </div>
  }
}
