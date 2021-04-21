import {Component, EventEmitter, h, Host, Method, Prop, State, Watch, Event} from '@stencil/core';
import moment from 'moment';
import {enter, leave, toggle} from 'el-transition'
import {registerClickOutside} from "stencil-click-outside";
import {
  ActivityBlueprint,
  ActivityDescriptor, PagedList, WorkflowBlueprint, WorkflowExecutionLogRecord,
} from "../../../../models";
import {activityIconProvider} from "../../../../services/activity-icon-provider";
import {createElsaClient} from "../../../../services/elsa-client";

interface Tab {
  id: string;
  text: string;
  view: () => any;
}

@Component({
  tag: 'elsa-workflow-instance-journal',
  shadow: false,
})
export class ElsaWorkflowInstanceJournal {

  constructor() {
    this.tabs = [{
      id: 'journal',
      text: 'Journal',
      view: this.renderJournalTab
    }, {
      id: 'activityState',
      text: 'Activity State',
      view: this.renderActivityStateTab
    }];
  }

  @Prop() workflowInstanceId: string;
  @Prop() serverUrl: string;
  @Prop() activityDescriptors: Array<ActivityDescriptor> = [];
  @Prop() workflowBlueprint: WorkflowBlueprint;
  @Event() recordSelected: EventEmitter<WorkflowExecutionLogRecord>;
  @State() isVisible: boolean = true;
  @State() records: PagedList<WorkflowExecutionLogRecord> = {items: [], totalCount: 0};
  @State() selectedRecordId?: string;
  @State() selectedTabId: string = 'journal';

  el: HTMLElement;

  tabs: Array<Tab> = [];

  @Method()
  async show() {
    this.isVisible = true;

    enter(this.el);
  }

  @Method()
  async hide() {
    leave(this.el).then(() => this.isVisible = false);
  }

  @Watch('workflowInstanceId')
  async workflowInstanceIdChangedHandler(newValue: string) {
    const workflowInstanceId = newValue;
    const client = createElsaClient(this.serverUrl);

    if (workflowInstanceId && workflowInstanceId.length > 0) {
      try {
        this.records = await client.workflowExecutionLogApi.get(workflowInstanceId);
      } catch {
        console.warn(`The specified workflow definition does not exist. Creating a new one.`);
      }
    }
  }

  async componentWillLoad() {
    await this.workflowInstanceIdChangedHandler(this.workflowInstanceId);
  }

  getEventColor(eventName: string) {
    const map = {
      'Executing': 'blue',
      'Executed': 'green',
      'Faulted': 'rose',
      'Warning': 'yellow',
      'Information': 'blue',
    };

    return map[eventName] || 'gray';
  }

  onCloseClick() {
    this.hide();
  }

  onShowClick() {
    this.show();
  }

  onRecordClick(record: WorkflowExecutionLogRecord) {
    this.selectedRecordId = record.id;
    this.recordSelected.emit(record);
  }
  
  onTabClick(e: Event, tab: Tab) {
    e.preventDefault();
    
    this.selectedTabId = tab.id;
  }

  render() {
    return (
      <Host>
        
        {this.renderPanel()}
      </Host>
    );
  }

  renderJournalButton() {
    return (
      <button onClick={() => this.onShowClick()} type="button"
                    class="workflow-settings-button fixed top-20 right-12 inline-flex items-center p-2 rounded-full border border-transparent bg-white shadow text-gray-400 hover:text-blue-500 focus:text-blue-500 hover:ring-2 hover:ring-offset-2 hover:ring-blue-500 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500">
      <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" stroke="currentColor" fill="none" class="h-8 w-8">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
              d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z"/>
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"/>
      </svg>
    </button>
    );
  }

  renderPanel() {
    
    const panelHiddenClass = this.isVisible ? '' : 'hidden';
    const tabs = this.tabs;
    const selectedTabId = this.selectedTabId;
    const selectedTab = tabs.find(x => x.id === selectedTabId);

    return (
      <section class={`${panelHiddenClass} fixed top-0 right-0 bottom-0 overflow-hidden`} aria-labelledby="slide-over-title" role="dialog" aria-modal="true">
        <div class="absolute inset-0 overflow-hidden">
          <div class="absolute inset-0" aria-hidden="true"/>
          <div class="fixed inset-y-0 right-0 pl-10 max-w-full flex sm:pl-16">

            <div ref={el => this.el = el}
                 data-transition-enter="transform transition ease-in-out duration-500 sm:duration-700"
                 data-transition-enter-start="translate-x-full"
                 data-transition-enter-end="translate-x-0"
                 data-transition-leave="transform transition ease-in-out duration-500 sm:duration-700"
                 data-transition-leave-start="translate-x-0"
                 data-transition-leave-end="translate-x-full"
                 class="w-screen max-w-2xl">
              <div class="h-full flex flex-col py-6 bg-white shadow-xl overflow-y-scroll">
                <div class="px-4 sm:px-6">
                  <div class="flex flex-col items-end ">
                    <div class="ml-3 h-7 flex items-center">
                      <button type="button" onClick={e => this.onCloseClick()} class="bg-white rounded-md text-gray-400 hover:text-gray-500 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500">
                        <span class="sr-only">Close panel</span>
                        <svg class="h-6 w-6" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
                        </svg>
                      </button>
                    </div>
                  </div>

                  <div>
                    <div>
                      <div class="border-b border-gray-200">
                        <nav class="-mb-px flex space-x-8" aria-label="Tabs">
                          {tabs.map(tab => {
                            const className = tab.id == selectedTabId ? 'border-blue-500 text-blue-600' : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300';
                            return <a href="#" onClick={e => this.onTabClick(e, tab)} class={`${className} whitespace-nowrap py-4 px-1 border-b-2 font-medium text-sm`}>{tab.text}</a>;
                          })}
                        </nav>
                      </div>
                    </div>
                  </div>

                </div>
                <div class="mt-6 relative flex-1 px-4 sm:px-6">
                  <div class="absolute inset-0 px-4 sm:px-6">
                    {selectedTab.view()}
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>
    );
  }

  renderJournalTab = () => {
    const records = this.records;
    const items = records.items;
    const activityDescriptors = this.activityDescriptors;
    const workflowBlueprint = this.workflowBlueprint;
    const activityBlueprints: Array<ActivityBlueprint> = workflowBlueprint.activities || [];
    const selectedRecordId = this.selectedRecordId;

    const renderRecord = (record: WorkflowExecutionLogRecord, index: number) => {
      const isLastItem = index == items.length - 1;
      const nextItem = isLastItem ? null : items[index + 1];
      const currentTimestamp = moment(record.timestamp);
      const nextTimestamp = isLastItem ? null : moment(nextItem.timestamp);
      const deltaTime = isLastItem ? null : moment.duration(nextTimestamp.diff(currentTimestamp));
      const activityType = record.activityType;
      const activityIcon = activityIconProvider.getIcon(activityType);
      const activityDescriptor = activityDescriptors.find(x => x.type === activityType);
      const activityBlueprint = activityBlueprints.find(x => x.id === record.activityId) || {name: null, displayName: null};
      const activityName = activityBlueprint.displayName || activityBlueprint.name || activityDescriptor.displayName || activityDescriptor.type;
      const eventName = record.eventName;
      const eventColor = this.getEventColor(eventName);
      const recordClass = record.id === selectedRecordId ? 'border-blue-600' : 'hover:bg-gray-100 border-transparent';

      const deltaTimeText = !!deltaTime ? deltaTime.asHours() > 1
        ? `${deltaTime.asHours()} h`
        : deltaTime.asMinutes() > 1
          ? `${deltaTime.asMinutes()} m`
          : deltaTime.asSeconds() > 1
            ? `${deltaTime.asSeconds()} s`
            : `${deltaTime.asMilliseconds()} ms`
        : null;

      return (
        <li>
          <div onClick={() => this.onRecordClick(record)} class={`${recordClass} border-2 cursor-pointer p-4 rounded`}>
            <div class="relative pb-10">
              {isLastItem ? undefined : <div class="flex absolute top-8 left-4 -ml-px h-full w-0.5 bg-gray-200">
                <div class="flex flex-1 items-center relative -left-5">
                  <span class="flex-1 text-sm text-gray-500 w-max bg-white p-1 rounded">{deltaTimeText}</span>
                </div>
              </div>}
              <div class="relative flex space-x-3">
                <div>
                  <span class="h-8 w-8 rounded-full bg-green-500 flex items-center justify-center ring-8 ring-white">
                    {activityIcon}
                  </span>
                </div>
                <div class="min-w-0 flex-1 pt-1.5 flex justify-between space-x-4">
                  <div>
                    <h3 class="text-lg leading-6 font-medium text-gray-900">
                      {activityName}
                    </h3>
                  </div>
                  <div>
                    <span class="relative inline-flex items-center rounded-full border border-gray-300 px-3 py-0.5 text-sm">
                      <span class="absolute flex-shrink-0 flex items-center justify-center">
                        <span class={`h-1.5 w-1.5 rounded-full bg-${eventColor}-500`} aria-hidden="true"/>
                      </span>
                      <span class="ml-3.5 font-medium text-gray-900">{eventName}</span>
                    </span>
                  </div>
                  <div class="text-right text-sm whitespace-nowrap text-gray-500">
                    <span>{currentTimestamp.format('DD-MM-YYYY HH:mm:ss')}</span>
                  </div>
                </div>
              </div>
              <div class="ml-12 mt-2">
                <dl class="grid grid-cols-1 gap-x-4 gap-y-8 sm:grid-cols-2">
                  <div class="sm:col-span-2">
                    <dt class="text-sm font-medium text-gray-500">
                      Activity ID
                    </dt>
                    <dd class="mt-1 text-sm text-gray-900">
                      {record.activityId}
                    </dd>
                  </div>
                  {record.message ? (
                    <div class="sm:col-span-2">
                      <dt class="text-sm font-medium text-gray-500">
                        Message
                      </dt>
                      <dd class="mt-1 text-sm text-gray-900">
                        {record.message}
                      </dd>
                    </div>
                  ) : undefined}
                </dl>
              </div>
            </div>
          </div>
        </li>
      );
    };

    return (
      <div class="flow-root">
        <ul class="-mb-8">
          {items.map(renderRecord)}
        </ul>
      </div>
    );
  };

  renderActivityStateTab = () => {
    return (
      <div>
        <pre>Code formatted activity state goes here...</pre>
      </div>
    );
  };
}
