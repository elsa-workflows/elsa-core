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

  private findActivityDescriptor = (): ActivityDescriptor => {
    const activity = this.activity;

    if(!activity) return null;

    const descriptor = descriptorsStore.activityDescriptors.find(x => x.typeName == activity.type && x.version == activity.version);
    return descriptor ?? descriptorsStore.activityDescriptors.sort((a, b) => b.version - a.version).find(x => x.typeName == this.activity.type);
  };
  private onSelectedTabIndexChanged = (e: CustomEvent<TabChangedArgs>) => this.selectedTabIndex = e.detail.selectedTabIndex

  private renderPropertiesTab = () => {
    const activity = this.activity;
    const activityDescriptor = this.findActivityDescriptor();
    const properties = activityDescriptor.inputs.filter(x => x.isBrowsable);
    const activityId = activity.id;
    const displayText: string = activity.metadata?.displayText ?? '';
    const executionLogEntry = this.activityExecutionLog;
    const activityState = executionLogEntry?.activityState ?? {};

    const propertyDetails: Lookup<string> = {
      'Activity ID': activityId,
      'Display Text': displayText
    };

    for (const property of properties) {
      const loggedPropName = property.name;
      const propertyValue = activityState[loggedPropName];
      const propertyValueText = (propertyValue !== null && typeof propertyValue === 'object') ? JSON.stringify(propertyValue) : (propertyValue != null ? propertyValue.toString() : '');
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
    const statusColor = log.eventName == "Completed" ? "tw-bg-blue-100" : log.eventName == "Faulted" ? "tw-bg-red-100" : "tw-bg-green-100";
    const icon = this.iconRegistry.getOrDefault(log.activityType)({size: ActivityIconSize.Small});
    return (
      <div class="tw-border-2 tw-cursor-pointer tw-p-4 tw-rounded">
        <div class="tw-relative tw-pb-10">
          <div class="tw-relative tw-flex tw-space-x-3">
            <div>
              <span class={`tw-h-8 tw-w-8 tw-rounded tw-p-1 tw-bg-blue-500 tw-flex tw-items-center tw-justify-center tw-ring-8 tw-ring-white tw-mr-1`}>
                {icon}
              </span>
            </div>
            <div class="tw-min-w-0 tw-flex-1 tw-pt-1.5 tw-flex tw-justify-between tw-space-x-4">
              <div>
                <h3 class="tw-text-lg tw-leading-6 tw-font-medium tw-text-gray-900">
                  {log.activityType}
                </h3>
              </div>
              <div>
                <span
                  class={`tw-relative tw-inline-flex tw-items-center tw-rounded-full ${statusColor} tw-border tw-border-gray-300 tw-px-3 tw-py-0.5 tw-text-sm`}>
                  <span class="tw-absolute tw-flex-shrink-0 tw-flex tw-items-center tw-justify-center">
                    <span class={`tw-h-1.5 tw-w-1.5 tw-rounded-full`} aria-hidden="true"/>
                  </span>
                  <span class="tw-font-medium tw-text-gray-900">{log.eventName}</span>
                </span>
              </div>
              <div class="tw-text-right tw-text-sm tw-whitespace-nowrap tw-text-gray-500">
                <span>{moment(log.timestamp).format('DD-MM-YYYY HH:mm:ss')}</span>
              </div>
            </div>
          </div>
          <div class="tw-ml-12 tw-mt-2">
            <dl class="sm:tw-divide-y sm:tw-divide-gray-200">
              <div class="tw-grid tw-grid-cols-2 tw-gap-x-4 tw-gap-y-8 sm:tw-grid-cols-2">
                <div class="sm:tw-col-span-2">
                  <dt class="tw-text-sm tw-font-medium tw-text-gray-500">
                    <span>Activity ID</span>
                    <copy-button value={log.activityId}/>
                  </dt>
                  <dd class="tw-mt-1 tw-text-sm tw-text-gray-900 tw-mb-2">{log.activityId}</dd>
                </div>
                {!!exception ? (
                  [<div class="sm:tw-col-span-2">
                    <dt class="tw-text-sm tw-font-medium tw-text-gray-500">
                      <span>Exception</span>
                      <copy-button value={exception.Type + '\n' + exception.Message}/>
                    </dt>
                    <dd class="tw-mt-1 tw-text-sm tw-text-gray-900">
                      {exception.message}
                    </dd>
                  </div>,
                    <div class="sm:tw-col-span-2">
                      <dt class="tw-text-sm tw-font-medium tw-text-gray-500">
                        <span>Exception Details</span>
                        <copy-button value={JSON.stringify(exception, null, 1)}/>
                      </dt>
                      <dd class="tw-mt-1 tw-text-sm tw-text-gray-900 tw-overflow-x-auto">
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
