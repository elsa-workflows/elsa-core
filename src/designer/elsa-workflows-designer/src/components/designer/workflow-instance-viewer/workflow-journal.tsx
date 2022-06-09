import {Component, h, Prop, State, Watch} from "@stencil/core";
import {ActionDefinition, ActionType, ActivityDescriptor, ActivityMetadata, WorkflowDefinition, WorkflowExecutionLogRecord, WorkflowInstance} from "../../../models";
import {Container} from "typedi";
import {ElsaApiClientProvider} from "../../../services";
import {durationToString, formatTime, formatTimestamp, getDuration, isNullOrWhitespace} from "../../../utils";
import {ActivityNode, flatten, walkActivities} from "../../activities/flowchart/activity-walker";
import descriptorsStore from '../../../data/descriptors-store';
import {ActivityExecutionEventBlock} from "./models";
import {start} from "@stencil/core/dev-server";

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
  @State() activityExecutionEventBlocks: Array<ActivityExecutionEventBlock> = [];

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
    const eventBlocks = this.activityExecutionEventBlocks;

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

                <table>
                  <thead>
                  <tr>
                    <th>Time</th>
                    <th>Activity</th>
                    <th>Status</th>
                    <th>Duration</th>
                  </tr>
                  </thead>

                  <tbody class="bg-white divide-y divide-gray-100">
                  {eventBlocks.map((eventBlock, index) => {
                    const activityNode = activityNodes.find(x => x.activity.id == eventBlock.activityId);
                    const activity = activityNode.activity;
                    const activityDescriptor = activityDescriptors.find(x => x.activityType == activityNode.activity.typeName);
                    const activityMetadata = activity.metadata;
                    const activityDisplayText = isNullOrWhitespace(activityMetadata.displayText) ? activity.typeName : activityMetadata.displayText;
                    const duration = durationToString(eventBlock.duration);
                    const status = eventBlock.completed ? 'Completed' : 'Started';

                    return (
                      <tr>
                        <td>{formatTime(eventBlock.timestamp)}</td>
                        <td>
                          <div>{activityDisplayText}</div>
                          <div class="font-bold">{activityDescriptor.activityType}</div>
                        </td>
                        <td>
                          <span class="inline-flex rounded-full bg-green-100 px-2 text-xs font-semibold leading-5 text-green-800">{status}</span>
                        </td>
                        <td>{duration}</td>
                      </tr>
                    );
                  })}
                  </tbody>
                </table>
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
    const blocks = this.groupRecords(pageOfRecords.items);
    this.workflowExecutionLogRecords = [...this.workflowExecutionLogRecords, ...pageOfRecords.items];
    this.activityExecutionEventBlocks = [...this.activityExecutionEventBlocks, ...blocks];
  }

  private groupRecords = (records: Array<WorkflowExecutionLogRecord>): Array<ActivityExecutionEventBlock> => {
    const startedEvents = records.filter(x => x.eventName == 'Started');
    const completedEvents = records.filter(x => x.eventName == 'Completed');

    const blocks: Array<ActivityExecutionEventBlock> = startedEvents.map(startedRecord => {
      const completedRecord = completedEvents.find(x => x.activityInstanceId == startedRecord.activityInstanceId);
      const duration = !!completedRecord ? getDuration(completedRecord.timestamp, startedRecord.timestamp) : null;

      return {
        activityId: startedRecord.activityId,
        completed: !!completedRecord,
        timestamp: startedRecord.timestamp,
        duration: duration,
        startedRecord: startedRecord,
        completedRecord: completedRecord
      }
    });

    return blocks;
  };
}
