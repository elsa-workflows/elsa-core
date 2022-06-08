import {Component, h, Prop, State, Watch} from "@stencil/core";
import {ActionDefinition, ActionType, ActivityDescriptor, ActivityMetadata, WorkflowDefinition, WorkflowExecutionLogRecord, WorkflowInstance} from "../../../models";
import {Container} from "typedi";
import {ElsaApiClientProvider} from "../../../services";
import {formatTime, formatTimestamp, isNullOrWhitespace} from "../../../utils";
import {ActivityNode, flatten, walkActivities} from "../../activities/flowchart/activity-walker";
import descriptorsStore from '../../../data/descriptors-store';

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

  @Prop() workflowInstance: WorkflowInstance;
  @Prop() workflowDefinition: WorkflowDefinition;
  @State() activityNodes: Array<ActivityNode> = [];
  @State() workflowExecutionLogRecords: Array<WorkflowExecutionLogRecord> = [];

  @Watch('workflowInstance')
  async onWorkflowInstanceChanged(value: string) {
    this.createGraph();
    await this.loadJournalPage(0);
  }

  @Watch('workflowDefinition')
  async onWorkflowDefinitionChanged(value: string) {
    this.createGraph();
    await this.loadJournalPage(0);
  }

  async componentWillLoad() {
    this.createGraph();
    await this.loadJournalPage(0);
  }

  public render() {
    const workflowInstance = this.workflowInstance;
    const workflowDefinition = this.workflowDefinition;
    const activityNodes = this.activityNodes;
    const activityDescriptors: Array<ActivityDescriptor> = descriptorsStore.activityDescriptors;
    const records = this.workflowExecutionLogRecords;

    return (

      <div class="absolute inset-0 overflow-hidden">
        <div class="h-full flex flex-col bg-white shadow-xl">
          <div class="flex flex-col flex-1">

            <div class="px-4 py-6 bg-gray-50 sm:px-6">
              <div class="flex items-start justify-between space-x-3">
                <div class="space-y-1">
                  <h2 class="text-lg font-medium text-gray-900">
                    Workflow Journal
                  </h2>
                </div>
              </div>
            </div>

            <div class="flex-1 relative">
              <div class="absolute inset-0 overflow-y-scroll">

                <ul role="list" class="m-4">
                  {records.map((record, index) => {
                    const isLastRecord = index == records.length - 1;
                    // const activityNode = activityNodes.find(x => x.activity.id == record.activityId);
                    // const activity = activityNode.activity;
                    // const activityDescriptor = activityDescriptors.find(x => x.activityType == activityNode.activity.typeName);
                    // const activityMetadata = activity.metadata;
                    // const activityDisplayText = isNullOrWhitespace(activityMetadata.displayText) ? activity.typeName : activityMetadata.displayText;
                    const activityDisplayText = 'Test';

                    return (
                      <li>
                        <div class="relative pb-8">
                          {isLastRecord ? undefined : <span class="absolute top-4 left-4 -ml-px h-full w-0.5 bg-gray-200" aria-hidden="true"/>}
                          <div class="relative flex space-x-3">
                            <div>
                              <span class="h-8 w-8 rounded-full bg-green-500 flex items-center justify-center ring-8 ring-white">
                                <svg class="h-5 w-5 text-white" x-description="Heroicon name: solid/check" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
                                  <path fill-rule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clip-rule="evenodd"/>
                                </svg>
                              </span>
                            </div>
                            <div class="min-w-0 flex-1 pt-1.5 flex justify-between space-x-4">
                              <div>
                                <p class="text-sm text-gray-500">{record.activityId}</p>
                                <div class="mt-2 text-sm text-gray-700">
                                  <p>
                                    {activityDisplayText}
                                  </p>
                                </div>
                              </div>
                              <div class="justify-self-end">
                                <a href="#" class="relative inline-flex items-center rounded-full border border-gray-300 px-3 py-0.5 text-sm">
                                    <span class="absolute flex-shrink-0 flex items-center justify-center">
                                      <span class="h-1.5 w-1.5 rounded-full bg-rose-500" aria-hidden="true"/>
                                    </span>
                                  <span class="ml-3.5 font-medium text-gray-900">{record.eventName}</span>
                                </a>
                              </div>
                              <div class="text-right text-sm whitespace-nowrap text-gray-500">
                                <time dateTime="2020-09-20">{formatTime(record.timestamp)}</time>
                              </div>
                            </div>
                          </div>
                        </div>
                      </li>
                    );
                  })}

                </ul>
              </div>
            </div>
          </div>

        </div>

      </div>
    );
  }

  private createGraph = () => {
    if (!this.workflowInstance || !this.workflowDefinition)
      return;

    const activityDescriptors = descriptorsStore.activityDescriptors;
    const rootNode = walkActivities(this.workflowDefinition.root, activityDescriptors);
    const nodes = flatten(rootNode);
    this.activityNodes = nodes;
  };

  private loadJournalPage = async (page: number): Promise<void> => {
    if (!this.workflowInstance || !this.workflowDefinition)
      return;

    const client = await this.elsaApiClientProvider.getElsaClient();
    const workflowInstanceId = this.workflowInstance.id;
    const pageOfRecords = await client.workflowInstances.getJournal({page, pageSize: PAGE_SIZE, workflowInstanceId: workflowInstanceId})
    this.workflowExecutionLogRecords = [...this.workflowExecutionLogRecords, ...pageOfRecords.items];
  }
}
