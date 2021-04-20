import {Component, h, Host, Method, Prop, State, Watch} from '@stencil/core';
import {
  ActivityDescriptor, PagedList, WorkflowExecutionLogRecord,
} from "../../../../models";
import {createElsaClient} from "../../../../services/elsa-client";

@Component({
  tag: 'elsa-workflow-instance-journal',
  shadow: false,
})
export class ElsaWorkflowInstanceJournal {

  @Prop() workflowInstanceId: string;
  @Prop() serverUrl: string;
  @Prop() activityDescriptors: Array<ActivityDescriptor> = [];
  @State() records: PagedList<WorkflowExecutionLogRecord> = {items: [], totalCount: 0};

  @Method()
  async getServerUrl(): Promise<string> {
    return this.serverUrl;
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

  render() {
    const records = this.records;
    const items = records.items;

    const renderRecord = (record: WorkflowExecutionLogRecord, index: number) => {
      const isLastItem = index == items.length - 1;
      return (
        <li>
          <div class="relative pb-8">
            {isLastItem ? undefined : <span class="absolute top-4 left-4 -ml-px h-full w-0.5 bg-gray-200" aria-hidden="true"/>}
            <div class="relative flex space-x-3">
              <div>
                  <span class="h-8 w-8 rounded-full bg-green-500 flex items-center justify-center ring-8 ring-white">
                    
                    <svg class="h-5 w-5 text-white" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
                      <path fill-rule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clip-rule="evenodd"/>
                    </svg>
                  </span>
              </div>
              <div class="min-w-0 flex-1 pt-1.5 flex justify-between space-x-4">
                <div>
                  <p class="text-sm text-gray-500">Completed phone screening with <a href="#" class="font-medium text-gray-900">Martha Gardner</a></p>
                </div>
                <div class="text-right text-sm whitespace-nowrap text-gray-500">
                  <time dateTime="2020-09-28">Sep 28</time>
                </div>
              </div>
            </div>
          </div>
        </li>
      );
    };

    return (
      <section class="fixed inset-0 overflow-hidden" aria-labelledby="slide-over-title" role="dialog" aria-modal="true">
        <div class="absolute inset-0 overflow-hidden">

          <div class="absolute inset-0" aria-hidden="true"/>

          <div class="fixed inset-y-0 right-0 pl-10 max-w-full flex sm:pl-16">

            <div class="w-screen max-w-2xl">
              <div class="h-full flex flex-col py-6 bg-white shadow-xl overflow-y-scroll">
                <div class="px-4 sm:px-6">
                  <div class="flex items-start justify-between">
                    <h2 class="text-lg font-medium text-gray-900" id="slide-over-title">
                      Workflow Journal
                    </h2>
                    <div class="ml-3 h-7 flex items-center">
                      <button class="bg-white rounded-md text-gray-400 hover:text-gray-500 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500">
                        <span class="sr-only">Close panel</span>

                        <svg class="h-6 w-6" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
                        </svg>
                      </button>
                    </div>
                  </div>
                </div>
                <div class="mt-6 relative flex-1 px-4 sm:px-6">

                  <div class="absolute inset-0 px-4 sm:px-6">

                    <div class="flow-root">
                      <ul class="-mb-8">
                        {items.map(renderRecord)}
                      </ul>
                    </div>

                  </div>

                </div>
              </div>
            </div>
          </div>
        </div>
      </section>
    );
  }
}
