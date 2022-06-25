import {Component, h, Prop, State, Watch} from "@stencil/core";
import {ActionDefinition, ActionType, Activity, ActivityDescriptor, ActivityMetadata, WorkflowDefinition, WorkflowExecutionLogRecord, WorkflowInstance} from "../../../models";
import {Container} from "typedi";
import {ActivityIconRegistry, ActivityNode, createActivityMap, createActivityNodeMap, ElsaApiClientProvider, flatten, walkActivities} from "../../../services";
import {durationToString, formatTime, formatTimestamp, getDuration, Hash, isNullOrWhitespace} from "../../../utils";
import descriptorsStore from '../../../data/descriptors-store';
import {ActivityExecutionEventBlock} from "./models";
import {ActivityIconSize} from "../../icons/activities";

const PAGE_SIZE: number = 20;

@Component({
  tag: 'elsa-workflow-journal',
  shadow: false,
  styleUrl: 'workflow-journal.scss'
})
export class WorkflowJournal {
  private readonly elsaApiClientProvider: ElsaApiClientProvider;
  private readonly iconRegistry: ActivityIconRegistry;

  constructor() {
    this.elsaApiClientProvider = Container.get(ElsaApiClientProvider);
    this.iconRegistry = Container.get(ActivityIconRegistry);
  }

  @Prop() workflowInstance: WorkflowInstance;
  @Prop() workflowDefinition: WorkflowDefinition;
  @State() nodeMap: Hash<ActivityNode> = {};
  @State() nodes: Array<ActivityNode> = [];
  @State() workflowExecutionLogRecords: Array<WorkflowExecutionLogRecord> = [];
  @State() activityExecutionEventBlocks: Array<ActivityExecutionEventBlock> = [];
  @State() rootBlocks: Array<ActivityExecutionEventBlock> = [];
  @State() expandedBlocks: Array<ActivityExecutionEventBlock> = [];

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

  render() {
    const workflowInstance = this.workflowInstance;
    const workflowDefinition = this.workflowDefinition;
    const nodeMap = this.nodeMap;
    const activityDescriptors: Array<ActivityDescriptor> = descriptorsStore.activityDescriptors;
    const blocks = this.rootBlocks;
    const iconRegistry = this.iconRegistry;
    const expandedBlocks = this.expandedBlocks;

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

                <table class="workflow-journal-table">
                  <thead>
                  <tr>
                    <th class="w-1"/>
                    <th>Time</th>
                    <th class="min-w-full">Activity</th>
                    <th>Status</th>
                    <th>Duration</th>
                  </tr>
                  </thead>

                  <tbody class="bg-white divide-y divide-gray-100">
                  {this.renderBlocks()}
                  </tbody>
                </table>
              </div>
            </div>
          </div>
        </div>
      </div>
    );
  }

  private renderBlocks = (activityId?: string) => {
    const nodeMap = this.nodeMap;
    const iconRegistry = this.iconRegistry;
    const expandedBlocks = this.expandedBlocks;
    const blocks = this.findChildBlocks(activityId);

    return blocks.map((block, index) => {
      const activityNode = nodeMap[block.activityId];
      const activity = activityNode.activity;
      const activityMetadata = activity.metadata;
      const activityDisplayText = isNullOrWhitespace(activityMetadata.displayText) ? activity.id : activityMetadata.displayText;
      const duration = durationToString(block.duration);
      const status = block.completed ? 'Completed' : 'Started';
      const icon = iconRegistry.get(activity.typeName)({size: ActivityIconSize.Small});
      const expanded = !!expandedBlocks.find(x => x == block);

      const toggleIcon = expanded
        ? (
          <svg class="h-6 w-6 text-gray-500" width="24" height="24" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor" fill="none" stroke-linecap="round" stroke-linejoin="round">
            <path stroke="none" d="M0 0h24v24H0z"/>
            <rect x="4" y="4" width="16" height="16" rx="2"/>
            <line x1="9" y1="12" x2="15" y2="12"/>
          </svg>
        )
        : (
          <svg class="h-6 w-6 text-gray-500" width="24" height="24" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor" fill="none" stroke-linecap="round" stroke-linejoin="round">
            <path stroke="none" d="M0 0h24v24H0z"/>
            <rect x="4" y="4" width="16" height="16" rx="2"/>
            <line x1="9" y1="12" x2="15" y2="12"/>
            <line x1="12" y1="9" x2="12" y2="15"/>
          </svg>
        );

      return (
        [<tr>
          <td class="w-1">
            {block.children.length > 0 ? (<a href="#" onClick={e => this.onBlockClick(e, block)}>
              {toggleIcon}
            </a>) : undefined}
          </td>
          <td>{formatTime(block.timestamp)}</td>
          <td class="min-w-full">
            <div class="flex items-center space-x-1">
              <div class="flex-shrink">
                <div class="bg-blue-500 rounded p-1">
                  {icon}
                </div>
              </div>
              <div>{activityDisplayText}</div>
            </div>
          </td>
          <td>
            <span class="inline-flex rounded-full bg-green-100 px-2 text-xs font-semibold leading-5 text-green-800">{status}</span>
          </td>
          <td>{duration}</td>
        </tr>, expanded ? this.renderBlocks(block.activityId) : undefined]
      );
    });
  }

  private createGraph = () => {
    if (!this.workflowInstance || !this.workflowDefinition)
      return;

    this.nodes = flatten(walkActivities(this.workflowDefinition.root));
    this.nodeMap = createActivityNodeMap(this.nodes);
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
    this.rootBlocks = this.findChildBlocks(null);
  }

  private groupRecords = (records: Array<WorkflowExecutionLogRecord>): Array<ActivityExecutionEventBlock> => {
    const startedEvents = records.filter(x => x.eventName == 'Started');
    const completedEvents = records.filter(x => x.eventName == 'Completed');
    const nodeMap = this.nodeMap;

    const blocks: Array<ActivityExecutionEventBlock> = startedEvents.map(startedRecord => {
      const completedRecord = completedEvents.find(x => x.activityInstanceId == startedRecord.activityInstanceId);
      const duration = !!completedRecord ? getDuration(completedRecord.timestamp, startedRecord.timestamp) : null;
      const node = nodeMap[startedRecord.activityId];
      const parents = node.parents.map(x => x.activity.id);
      const children = node.children.map(x => x.activity.id);

      return {
        activityId: startedRecord.activityId,
        parents: parents,
        children: children,
        completed: !!completedRecord,
        timestamp: startedRecord.timestamp,
        duration: duration,
        startedRecord: startedRecord,
        completedRecord: completedRecord
      }
    });

    return blocks;
  };

  private findChildBlocks = (parentActivityId?: string): Array<ActivityExecutionEventBlock> => {
    const blocks = this.activityExecutionEventBlocks;

    if (blocks.length == 0)
      return [];

    if (!!parentActivityId)
      return blocks.filter(x => !!x.parents.find(p => p == parentActivityId));

    return blocks.filter(x => x.parents.length == 0);
  }

  private onBlockClick = (e: MouseEvent, block: ActivityExecutionEventBlock) => {
    e.preventDefault();

    const existingBlock = this.expandedBlocks.find(x => x == block);
    this.expandedBlocks = existingBlock ? this.expandedBlocks.filter(x => x != existingBlock) : [...this.expandedBlocks, block];
  };
}
