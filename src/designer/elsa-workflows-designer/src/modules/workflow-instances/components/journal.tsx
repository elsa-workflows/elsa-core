import {Component, h, Method, Prop, State, Watch, Event, EventEmitter} from "@stencil/core";
import {Activity, Workflow, WorkflowExecutionLogRecord, WorkflowInstance, Container as ActivityContainer} from "../../../models";
import {Container} from "typedi";
import {ActivityIconRegistry, ActivityNode, createActivityNodeMap, flatten, walkActivities} from "../../../services";
import {durationToString, formatTime, getDuration, Hash, isNullOrWhitespace} from "../../../utils";
import {ActivityExecutionEventBlock} from "../models";
import {ActivityIconSize} from "../../../components/icons/activities";
import {WorkflowDefinition} from "../../workflow-definitions/models/entities";
import {WorkflowInstancesApi} from "../services/workflow-instances-api";
import { JournalItemSelectedArgs } from "../events";

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

  @Prop() workflowInstance: WorkflowInstance;
  @Prop() workflowDefinition: WorkflowDefinition;
  @State() nodeMap: Hash<ActivityNode> = {};
  @State() nodes: Array<ActivityNode> = [];
  @State() workflowExecutionLogRecords: Array<WorkflowExecutionLogRecord> = [];
  @State() rootBlocks: Array<ActivityExecutionEventBlock> = [];
  @State() expandedBlocks: Array<ActivityExecutionEventBlock> = [];
  @State() journalActivityMap: Hash<Activity> = {};
  @Event() journalItemSelected: EventEmitter<JournalItemSelectedArgs>;

  @Watch('workflowInstance')
  async onWorkflowInstanceChanged(value: string) {
    this.createGraph();
    await this.loadJournalPage(0);
  }

  @Watch('workflowDefinition')
  async onWorkflowDefinitionChanged(value: string) {
    this.rootBlocks = [];
    this.createGraph();
    await this.loadJournalPage(0);
  }

  @Method()
  public async getExecutionLogByActivityId(activityId: string): Promise<WorkflowExecutionLogRecord> {
    const logRecords = this.workflowExecutionLogRecords.filter(r => r.activityId === activityId);
    return logRecords.find(r => r.eventName === "Faulted" || r.eventName === "Completed") ?? logRecords.find(r => r.eventName === "Started");
  }

  async componentWillLoad(): Promise<void> {
    this.createGraph();
    this.journalActivityMap = this.createActivityMapForJournal();
    await this.loadJournalPage(0);
  }

  render() {
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
    var sortedBlocks = this.sortByTimestamp(blocks);
    return sortedBlocks.map((block, index) => {
      const activity = journalActivityMap[block.activityId];

      if(activity.type == "Elsa.Workflow" || activity.type == "Elsa.Flowchart"){
        return this.renderBlocks(block.children);
      }

      const activityMetadata = activity.metadata;
      const activityDisplayText = isNullOrWhitespace(activityMetadata.displayText) ? activity.id : activityMetadata.displayText;
      const duration = durationToString(block.duration);
      const status = block.completed ? 'Completed' : block.faulted ? 'Faulted' : 'Started';
      const icon = iconRegistry.getOrDefault(activity.type)({size: ActivityIconSize.Small});
      const expanded = !!expandedBlocks.find(x => x == block);
      const statusColor = block.completed ? "bg-blue-100" : block.faulted ? "bg-red-100" : "bg-green-100";

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
          <td>
            <a href="#" onClick={e => this.onJournalItemClick(e, block, activity)}>{formatTime(block.timestamp)}</a>
          </td>
          <td class="min-w-full">
            <div class="flex items-center space-x-1">
              <div class="flex-shrink">
                <div class="bg-blue-500 rounded p-1">
                  <a href="#" onClick={e => this.onJournalItemClick(e, block, activity)}>{icon}</a>
                </div>
              </div>
              <div><a href="#" onClick={e => this.onJournalItemClick(e, block, activity)}>{activityDisplayText}</a></div>
            </div>
          </td>
          <td>
            <a href="#" onClick={e => this.onJournalItemClick(e, block, activity)} class={`inline-flex rounded-full ${statusColor} px-2 text-xs font-semibold leading-5 text-green-800`}>{status}</a>
          </td>
          <td><a href="#" onClick={e => this.onJournalItemClick(e, block, activity)}>{duration}</a></td>
        </tr>, expanded ? this.renderBlocks(block.children) : undefined]
      );
    });
  }

  private createGraph = () => {
    if (!this.workflowInstance || !this.workflowDefinition)
      return;

    const workflow: Workflow = {
      type: 'Elsa.Workflow',
      id: 'Workflow1', // Always 'Workflow1'.
      version: this.workflowDefinition.version,
      customProperties: this.workflowDefinition.customProperties,
      canStartWorkflow: false,
      runAsynchronously: false,
      metadata: {},
      root: this.workflowDefinition.root,
      variables: this.workflowDefinition.variables
    };

    this.nodes = flatten(walkActivities(workflow));
    this.nodeMap = createActivityNodeMap(this.nodes);
  };

  private loadJournalPage = async (page: number): Promise<void> => {
    if (!this.workflowInstance || !this.workflowDefinition)
      return;

    const workflowInstanceId = this.workflowInstance.id;
    const pageOfRecords = await this.workflowInstancesApi.getJournal({page, pageSize: PAGE_SIZE, workflowInstanceId: workflowInstanceId});
    const blocks = this.createBlocks(pageOfRecords.items);
    const rootBlocks = blocks.filter(x => !x.parentActivityInstanceId);
    this.workflowExecutionLogRecords = [...this.workflowExecutionLogRecords, ...pageOfRecords.items];
    this.rootBlocks = rootBlocks;
  }

  private createBlocks = (records: Array<WorkflowExecutionLogRecord>): Array<ActivityExecutionEventBlock> => {
    const startedEvents = records.filter(x => x.eventName == 'Started');
    const completedEvents = records.filter(x => x.eventName == 'Completed');
    const faultedEvents = records.filter(x => x.eventName == 'Faulted');

    const blocks = startedEvents.map(startedRecord => {
      const completedRecord = completedEvents.find(x => x.activityInstanceId == startedRecord.activityInstanceId);
      const faultedRecord = faultedEvents.find(x => x.activityInstanceId == startedRecord.activityInstanceId);
      const duration = !!completedRecord ? getDuration(completedRecord.timestamp, startedRecord.timestamp) : null;

      return {
        activityId: startedRecord.activityId,
        activityInstanceId: startedRecord.activityInstanceId,
        parentActivityInstanceId: startedRecord.parentActivityInstanceId,
        completed: !!completedRecord,
        faulted: !!faultedRecord,
        timestamp: startedRecord.timestamp,
        duration: duration,
        startedRecord: startedRecord,
        completedRecord: completedRecord,
        faultedRecord: faultedRecord,
        children: []
      };
    });

    for (const block of blocks) {
      block.children = this.findChildBlocks(blocks, block.activityInstanceId);
    }

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
    this.journalItemSelected.emit({activity: activity, activityInstanceId: block.activityInstanceId});
  };

  private sortByTimestamp(blocks: ActivityExecutionEventBlock[]) {
    return blocks.sort(function (x, y) {
      if(x.timestamp > y.timestamp) 
        return 1;
      return -1;
    });
  }

  private createActivityMapForJournal() {
    let allActivities = new Array<Activity>();
    allActivities.push({id: 'Workflow1', type: 'Elsa.Workflow', version: null, metadata: {}, customProperties: null});

    allActivities = this.walkActivitiesRecursive(this.workflowDefinition.root as ActivityContainer, allActivities);

    const map = {};
    for (const activity of allActivities)
      map[activity.id] = activity;
    return map;
  }
  
  private walkActivitiesRecursive(activityContainer: ActivityContainer, allActivities: Array<Activity>) {
    allActivities.push(activityContainer as Activity);
    if(activityContainer.activities == null || activityContainer.activities.length == 0) {
      if(activityContainer.root != null) {
        allActivities.push(activityContainer.root as Activity);
        allActivities = this.walkActivitiesRecursive(activityContainer.root, allActivities);
      }
    }
    else {
      allActivities = allActivities.concat(activityContainer.activities);
      activityContainer.activities.forEach(activity => {
        if(activity.root != null){
          allActivities = this.walkActivitiesRecursive(activity.root, allActivities);
        }
      });
    }
    return allActivities;
  }
}