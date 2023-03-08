import {Component, h, Method, Prop, State, Watch} from '@stencil/core';
import {camelCase} from 'lodash';
import {
  Activity,
  ActivityDescriptor,
  Lookup,
  TabChangedArgs,
  TabDefinition,
  WorkflowExecutionLogRecord
} from '../../../models';
import {InfoList} from "../../../components/shared/forms/info-list";
import descriptorsStore from "../../../data/descriptors-store";
import moment from 'moment';
import Container from 'typedi';
import { ActivityIconRegistry } from '../../../services';
import {ActivityIconSize} from "../../../components/icons/activities";

@Component({
  tag: 'elsa-activity-properties',
})
export class ActivityProperties {
  private slideOverPanel: HTMLElsaSlideOverPanelElement;
  private readonly iconRegistry: ActivityIconRegistry;

  constructor() {
    this.iconRegistry = Container.get(ActivityIconRegistry);
  }

  @Prop({mutable: true}) public activity?: Activity;
  @Prop() activityExecutionLog: WorkflowExecutionLogRecord;
  @Prop() activityPropertyTabIndex?: number;
  @State() private selectedTabIndex: number = 0;

  @Method()
  async show(): Promise<void> {
    await this.slideOverPanel.show();
  }

  @Method()
  async hide(): Promise<void> {
    await this.slideOverPanel.hide();
  }

  @Method()
  async updateSelectedTab(tabIndex : number): Promise<void> {
    this.selectedTabIndex = tabIndex;
  }

  @Watch('activity')


  async componentWillLoad(): Promise<void> {
    if(this.activityPropertyTabIndex != null) {
      this.selectedTabIndex = this.activityPropertyTabIndex;
    }
  }

  render() {
    const activity = this.activity;
    const activityDescriptor = this.findActivityDescriptor();

    const propertiesTab: TabDefinition = {
      displayText: 'Properties',
      content: () => this.renderPropertiesTab()
    };

    const commonTab: TabDefinition = {
      displayText: 'Common',
      content: () => this.renderCommonTab()
    };

    const journalTab: TabDefinition = {
      displayText: 'Journal',
      content: () => this.renderJournalTab()
    };

    const tabs = !!activityDescriptor ? [propertiesTab, commonTab, journalTab] : [];
    const mainTitle = activity.id;
    const subTitle = activityDescriptor.displayName;

    return (
      <elsa-form-panel
        mainTitle={mainTitle}
        subTitle={subTitle}
        tabs={tabs}
        selectedTabIndex={this.selectedTabIndex}
        onSelectedTabIndexChanged={e => this.onSelectedTabIndexChanged(e)}
      />
    );
  }

  private findActivityDescriptor = (): ActivityDescriptor => !!this.activity ? descriptorsStore.activityDescriptors.find(x => x.typeName == this.activity.type) : null;
  private onSelectedTabIndexChanged = (e: CustomEvent<TabChangedArgs>) => this.selectedTabIndex = e.detail.selectedTabIndex

  private renderPropertiesTab = () => {
    const activity = this.activity;
    const activityDescriptor = this.findActivityDescriptor();
    const properties = activityDescriptor.inputs;
    const activityId = activity.id;
    const displayText: string = activity.metadata?.displayText ?? '';
    const executionLogEntry = this.activityExecutionLog;
    const activityState = executionLogEntry?.activityState ?? {};

    const propertyDetails: Lookup<string> = {
      'Activity ID': activityId,
      'Display Text': displayText
    };

    for (const property of properties) {
      const propertyName = camelCase(property.name);
      const loggedPropName = property.name;
      const propertyValue = activityState[loggedPropName] ?? activity[propertyName]?.expression?.value;
      const propertyValueText = propertyValue != null ? propertyValue.toString() : '';
      propertyDetails[property.displayName] = propertyValueText;
    }

    return <div>
      <InfoList title="Properties" dictionary={propertyDetails}/>
    </div>
  };

  private renderCommonTab = () => {
    return <div>
    </div>
  };

  private renderJournalTab = () => {
    const log = this.activityExecutionLog;
    if(log == null) return;

    const exception = log.payload?.exception;
    const statusColor = log.eventName == "Completed" ? "bg-blue-100" : log.eventName == "Faulted" ? "bg-red-100" : "bg-green-100";
    const icon = this.iconRegistry.getOrDefault(log.activityType)({size: ActivityIconSize.Small});
    return (
      <div class="border-2 cursor-pointer p-4 rounded">
        <div class="relative pb-10">
          <div class="relative flex space-x-3">
            <div>
              <span class={`h-8 w-8 rounded p-1 bg-blue-500 flex items-center justify-center ring-8 ring-white mr-1`}>
                {icon}
              </span>
            </div>
            <div class="min-w-0 flex-1 pt-1.5 flex justify-between space-x-4">
              <div>
                <h3 class="text-lg leading-6 font-medium text-gray-900">
                  {log.activityType}
                </h3>
              </div>
              <div>
                <span
                  class={`relative inline-flex items-center rounded-full ${statusColor} border border-gray-300 px-3 py-0.5 text-sm`}>
                  <span class="absolute flex-shrink-0 flex items-center justify-center">
                    <span class={`h-1.5 w-1.5 rounded-full`} aria-hidden="true"/>
                  </span>
                  <span class="font-medium text-gray-900">{log.eventName}</span>
                </span>
              </div>
              <div class="text-right text-sm whitespace-nowrap text-gray-500">
                <span>{moment(log.timestamp).format('DD-MM-YYYY HH:mm:ss')}</span>
              </div>
            </div>
          </div>
          <div class="ml-12 mt-2">
            <dl class="sm:divide-y sm:divide-gray-200">
              <div class="grid grid-cols-2 gap-x-4 gap-y-8 sm:grid-cols-2">
                <div class="sm:col-span-2">
                  <dt class="text-sm font-medium text-gray-500">
                    <span>Activity ID</span>
                    <copy-button value={log.activityId}/>
                  </dt>
                  <dd class="mt-1 text-sm text-gray-900 mb-2">{log.activityId}</dd>
                </div>
                {!!exception ? (
                  [<div class="sm:col-span-2">
                    <dt class="text-sm font-medium text-gray-500">
                      <span>Exception</span>
                      <copy-button value={exception.Type + '\n' + exception.Message}/>
                    </dt>
                    <dd class="mt-1 text-sm text-gray-900">
                      {exception.message}
                    </dd>
                  </div>,
                    <div class="sm:col-span-2">
                      <dt class="text-sm font-medium text-gray-500">
                        <span>Exception Details</span>
                        <copy-button value={JSON.stringify(exception, null, 1)}/>
                      </dt>
                      <dd class="mt-1 text-sm text-gray-900 overflow-x-auto">
                        <pre>{JSON.stringify(exception, null, 1)}</pre>
                      </dd>
                    </div>]
                ) : undefined}
              </div>
            </dl>
          </div>
        </div>
      </div>
    )
  };
}
