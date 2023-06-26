import {Component, h, Prop, State, Watch, Event, EventEmitter, Method} from "@stencil/core";
import {Activity, Workflow, WorkflowExecutionLogRecord, WorkflowInstance} from "../../../models";
import {Container} from "typedi";
import {ActivityIconRegistry, ActivityNode, flatten, walkActivities} from "../../../services";
import {durationToString, formatTime, getDuration, Hash, isNullOrWhitespace} from "../../../utils";
import {ActivityExecutionEventBlock, WorkflowJournalModel} from "../models";
import {ActivityIconSize} from "../../../components/icons/activities";
import {WorkflowInstancesApi} from "../services/workflow-instances-api";
import {JournalItemSelectedArgs} from "../events";

// TODO: Implement dynamic loading of records.
const PAGE_SIZE: number = 10000;

@Component({
  tag: 'elsa-workflow-journal',
  shadow: false,
  styleUrl: 'journal.scss'
})
export class Journal {
  private readonly iconRegistry: ActivityIconRegistry;
  private readonly workflowInstancesApi: WorkflowInstancesApi;

  constructor() {
    this.iconRegistry = Container.get(ActivityIconRegistry);
    this.workflowInstancesApi = Container.get(WorkflowInstancesApi);
  }

  @Prop() model: WorkflowJournalModel;
  @State() workflowExecutionLogRecords: Array<WorkflowExecutionLogRecord> = [];
  @State() blocks: Array<ActivityExecutionEventBlock> = [];
  @State() rootBlocks: Array<ActivityExecutionEventBlock> = [];
  @State() expandedBlocks: Array<ActivityExecutionEventBlock> = [];
  @State() journalActivityMap: Set<ActivityNode> = new Set<ActivityNode>();
  @Event() journalItemSelected: EventEmitter<JournalItemSelectedArgs>;

  @Watch('model')
  async onWorkflowInstanceModelChanged(oldValue: WorkflowJournalModel, newValue: WorkflowJournalModel) {
    if(oldValue.workflowInstance.id != newValue.workflowInstance.id)
      await this.refresh();
  }

  async componentWillLoad(): Promise<void> {
    await this.refresh();
  }

  @Method()
  async refresh() {
    this.rootBlocks = [];
    await this.loadJournalPage(0);
    this.createActivityMapForJournal();
  }

  render() {
    return (

      <div class="tw-absolute tw-inset-0 tw-overflow-hidden">
        <div class="tw-h-full tw-flex tw-flex-col tw-bg-white tw-shadow-xl">
          <div class="tw-flex tw-flex-col tw-flex-1">

            <div class="tw-px-4 tw-py-6 tw-bg-gray-50 sm:tw-px-6">
              <div class="tw-flex tw-items-start tw-justify-between tw-space-x-3">
                <div class="tw-space-y-1">
                  <h2 class="tw-text-lg tw-font-medium tw-text-gray-900">
                    Workflow Journal
                  </h2>
                </div>
              </div>
            </div>

            <div class="tw-flex-1 tw-relative">
              <div class="tw-absolute tw-inset-0 tw-overflow-y-scroll">

                <table class="workflow-journal-table">
                  <thead>
                  <tr>
                    <th class="tw-w-1"/>
                    <th>Time</th>
                    <th class="tw-min-w-full">Activity</th>
                    <th>Status</th>
                    <th>Duration</th>
                  </tr>
                  </thead>

                  <tbody class="tw-bg-white tw-divide-y tw-divide-gray-100">
                  {this.renderBlocks(this.rootBlocks)}
                  </tbody>
                </table>
              </div>
            </div>
          </div>
        </div>
      </div>
    );
  }

  private renderBlocks = (blocks: Array<ActivityExecutionEventBlock>) => {
    const journalActivityMap = this.journalActivityMap;
    const iconRegistry = this.iconRegistry;
    const expandedBlocks = this.expandedBlocks;
    const sortedBlocks = this.sortByTimestamp(blocks);

    return sortedBlocks.map((block) => {
      const activityNode = journalActivityMap[block.nodeId];

      if (activityNode == null)
        debugger

      const activity = activityNode.activity;

      if (activity.type == "Elsa.Workflow" || activity.type == "Elsa.Flowchart")
        return this.renderBlocks(block.children);

      const activityMetadata = activity.metadata;
      const activityDisplayText = isNullOrWhitespace(activityMetadata.displayText) ? activity.id : activityMetadata.displayText;
      const duration = durationToString(block.duration);
      const status = block.completed ? 'Completed' : block.suspended ? 'Suspended' : block.faulted ? 'Faulted' : 'Started';
      const icon = iconRegistry.getOrDefault(activity.type)({size: ActivityIconSize.Small});
      const expanded = !!expandedBlocks.find(x => x == block);
      const statusColor = block.completed ? "tw-bg-blue-100" : block.faulted ? "tw-bg-red-100" : "tw-bg-green-100";

      const toggleIcon = expanded
        ? (
          <svg class="tw-h-6 tw-w-6 tw-text-gray-500" width="24" height="24" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor" fill="none" stroke-linecap="round" stroke-linejoin="round">
            <path stroke="none" d="M0 0h24v24H0z"/>
            <rect x="4" y="4" width="16" height="16" rx="2"/>
            <line x1="9" y1="12" x2="15" y2="12"/>
          </svg>
        )
        : (
          <svg class="tw-h-6 tw-w-6 tw-text-gray-500" width="24" height="24" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor" fill="none" stroke-linecap="round" stroke-linejoin="round">
            <path stroke="none" d="M0 0h24v24H0z"/>
            <rect x="4" y="4" width="16" height="16" rx="2"/>
            <line x1="9" y1="12" x2="15" y2="12"/>
            <line x1="12" y1="9" x2="12" y2="15"/>
          </svg>
        );

      return (
        [<tr>
          <td class="tw-w-1">
            {block.children.length > 0 ? (<a href="#" onClick={e => this.onBlockClick(e, block)}>
              {toggleIcon}
            </a>) : undefined}
          </td>
          <td>
            <a href="#" onClick={e => this.onJournalItemClick(e, block, activity)}>{formatTime(block.timestamp)}</a>
          </td>
          <td class="tw-min-w-full">
            <div class="tw-flex tw-items-center tw-space-x-1">
              <div class="tw-flex-shrink">
                <div class="tw-bg-blue-500 tw-rounded tw-p-1">
                  <a href="#" onClick={e => this.onJournalItemClick(e, block, activity)}>{icon}</a>
                </div>
              </div>
              <div><a href="#" onClick={e => this.onJournalItemClick(e, block, activity)}>{activityDisplayText}</a></div>
            </div>
          </td>
          <td>
            <a href="#" onClick={e => this.onJournalItemClick(e, block, activity)} class={`tw-inline-flex tw-rounded-full ${statusColor} tw-px-2 tw-text-xs tw-font-semibold tw-leading-5 tw-text-green-800`}>{status}</a>
          </td>
          <td><a href="#" onClick={e => this.onJournalItemClick(e, block, activity)}>{duration}</a></td>
        </tr>, expanded ? this.renderBlocks(block.children) : undefined]
      );
    }).filter(x => !!x);
  }

  private loadJournalPage = async (page: number): Promise<void> => {
    if (!this.model)
      return;

    const workflowInstance = this.model.workflowInstance;
    const workflowInstanceId = workflowInstance.id;
    const pageOfRecords = await this.workflowInstancesApi.getJournal({page, pageSize: PAGE_SIZE, workflowInstanceId: workflowInstanceId});
    const blocks = this.createBlocks(pageOfRecords.items);
    const rootBlocks = blocks.filter(x => !x.parentActivityInstanceId);
    this.workflowExecutionLogRecords = [...this.workflowExecutionLogRecords, ...pageOfRecords.items];
    this.rootBlocks = rootBlocks;
    this.blocks = this.sortByTimestamp(blocks);
  }

  private createBlocks = (records: Array<WorkflowExecutionLogRecord>): Array<ActivityExecutionEventBlock> => {
    const startedEvents = records.filter(x => x.eventName == 'Started');
    const completedEvents = records.filter(x => x.eventName == 'Completed');
    const faultedEvents = records.filter(x => x.eventName == 'Faulted');
    const suspendedEvents = records.filter(x => x.eventName == 'Suspended');

    const blocks = startedEvents.map(startedRecord => {
      const completedRecord = completedEvents.find(x => x.activityInstanceId == startedRecord.activityInstanceId);
      const faultedRecord = faultedEvents.find(x => x.activityInstanceId == startedRecord.activityInstanceId);
      const suspendedRecord = suspendedEvents.find(x => x.activityInstanceId == startedRecord.activityInstanceId);
      const duration = !!completedRecord ? getDuration(completedRecord.timestamp, startedRecord.timestamp) : null;

      return {
        nodeId: startedRecord.nodeId,
        activityId: startedRecord.activityId,
        activityInstanceId: startedRecord.activityInstanceId,
        parentActivityInstanceId: startedRecord.parentActivityInstanceId,
        completed: !!completedRecord,
        faulted: !!faultedRecord,
        suspended: !!suspendedRecord,
        timestamp: startedRecord.timestamp,
        duration: duration,
        startedRecord: startedRecord,
        completedRecord: completedRecord,
        faultedRecord: faultedRecord,
        suspendedRecord: suspendedRecord,
        children: []
      };
    });

    for (const block of blocks)
      block.children = this.findChildBlocks(blocks, block.activityInstanceId);

    return blocks;
  };

  private findChildBlocks = (blocks: Array<ActivityExecutionEventBlock>, parentActivityInstanceId?: string): Array<ActivityExecutionEventBlock> => {
    if (blocks.length == 0)
      return [];

    return !!parentActivityInstanceId
      ? blocks.filter(x => x.parentActivityInstanceId == parentActivityInstanceId)
      : blocks.filter(x => !x.parentActivityInstanceId);
  }

  private onBlockClick = (e: MouseEvent, block: ActivityExecutionEventBlock) => {
    e.preventDefault();

    const existingBlock = this.expandedBlocks.find(x => x == block);
    this.expandedBlocks = existingBlock ? this.expandedBlocks.filter(x => x != existingBlock) : [...this.expandedBlocks, block];
  };

  private onJournalItemClick = async (e: MouseEvent, block: ActivityExecutionEventBlock, activity: Activity) => {
    e.preventDefault();
    this.journalItemSelected.emit({activity: activity, executionEventBlock: block, activityNode: this.journalActivityMap[block.nodeId]});
  };

  private sortByTimestamp(blocks: ActivityExecutionEventBlock[]) {
    return blocks.sort(function (x, y) {
      if (x.timestamp > y.timestamp)
        return 1;
      return -1;
    });
  }

  private createActivityMapForJournal() {
    const workflowDefinition = this.model.workflowDefinition;

    // Create dummy root workflow to match structure of workflow execution log entries in order to generate the right node IDs.
    const workflow: Workflow = {
      type: 'Elsa.Workflow',
      version: workflowDefinition.version,
      id: "Workflow1",
      root: workflowDefinition.root,
      variables: workflowDefinition.variables,
      metadata: {},
      customProperties: {}
    }

    const graph = walkActivities(workflow);
    const nodes = flatten(graph);
    const map = new Set<ActivityNode>();

    for (const node of nodes)
      map[node.nodeId] = node;

    this.journalActivityMap = map;
    this.blocks = this.blocks.filter(x => !!map[x.nodeId]);
  }
}
