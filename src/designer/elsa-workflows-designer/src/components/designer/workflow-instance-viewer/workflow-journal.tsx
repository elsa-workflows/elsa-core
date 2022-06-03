import {Component, h, Prop, State, Watch} from "@stencil/core";
import {WorkflowExecutionLogRecord} from "../../../models";
import {Container} from "typedi";
import {ElsaApiClientProvider} from "../../../services";

const PAGE_SIZE: number = 20;

@Component({
  tag: 'elsa-workflow-journal',
  shadow: false
})
export class WorkflowJournal {
  private readonly elsaApiClientProvider: ElsaApiClientProvider;

  constructor() {
    this.elsaApiClientProvider = Container.get(ElsaApiClientProvider);
  }

  @Prop() workflowInstanceId;
  @State() workflowExecutionLogRecords: Array<WorkflowExecutionLogRecord> = [];

  @Watch('workflowInstanceId')
  async onWorkflowInstanceIdChanged(value: string) {
    await this.loadJournalPage(0);
  }

  async componentWillLoad() {
    await this.loadJournalPage(0);
  }

  public render() {

    const records = this.workflowExecutionLogRecords;

    return (

      <div>

        <div class="px-4 py-6 bg-gray-50 sm:px-6">
          <div class="flex items-start justify-between space-x-3">
            <div class="space-y-1">
              <h2 class="text-lg font-medium text-gray-900">
                Workflow Journal
              </h2>
            </div>
          </div>
        </div>

        <div class="flow-root p-4 overflow-hidden">
          <ul role="list" class="-mb-8">
            {records.map(record => (
              <li>
                <div class="relative pb-8">
                  <span class="absolute top-4 left-4 -ml-px h-full w-0.5 bg-gray-200" aria-hidden="true"/>
                  <div class="relative flex space-x-3">
                    <div>
                    <span class="h-8 w-8 rounded-full bg-gray-400 flex items-center justify-center ring-8 ring-white">

                      <svg class="h-5 w-5 text-white" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
                        <path fill-rule="evenodd" d="M10 9a3 3 0 100-6 3 3 0 000 6zm-7 9a7 7 0 1114 0H3z" clip-rule="evenodd"/>
                      </svg>
                    </span>
                    </div>
                    <div class="min-w-0 flex-1 pt-1.5 flex justify-between space-x-4">
                      <div>
                        <p class="text-sm text-gray-500">Applied to <a href="#" class="font-medium text-gray-900">Front End Developer</a></p>
                      </div>
                      <div class="text-right text-sm whitespace-nowrap text-gray-500">
                        <time dateTime="2020-09-20">Sep 20</time>
                      </div>
                    </div>
                  </div>
                </div>
              </li>
            ))}

          </ul>
        </div>
      </div>
    );
  }

  private loadJournalPage = async (page: number): Promise<void> => {
    if (!this.workflowInstanceId)
      return;

    const client = await this.elsaApiClientProvider.getElsaClient();
    const pageOfRecords = await client.workflowInstances.getJournal({page, pageSize: PAGE_SIZE, workflowInstanceId: this.workflowInstanceId})
    this.workflowExecutionLogRecords = [...this.workflowExecutionLogRecords, ...pageOfRecords.items];
  }
}
