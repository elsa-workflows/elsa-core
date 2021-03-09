import {Component, Host, h, Prop, State, Event, EventEmitter, Listen, Watch} from '@stencil/core';
import {Map, addConnection, findActivity, getChildActivities, getInboundConnections, getOutboundConnections, removeActivity} from '../../../../utils/utils';
import {cleanup, destroy, updateConnections} from '../../../../utils/jsplumb-helper';
import {ActivityDescriptor, ActivityDesignDisplayContext, ActivityModel, ConnectionModel, EventTypes, WorkflowModel} from "../../../../models";
import {eventBus} from '../../../../services/event-bus';
import jsPlumb from "jsplumb";
import uuid = jsPlumb.jsPlumbUtil.uuid;
import {ActivityIcon} from "../../../icons/activity-icon";

@Component({
  tag: 'elsa-designer-tree',
  styleUrls: ['elsa-designer-tree.css'],
  assetsDirs: ['assets'],
  shadow: false
})
export class ElsaWorkflowDesigner {

  @Prop() model: WorkflowModel = {activities: [], connections: []};
  @Event({eventName: 'workflow-changed', bubbles: true, composed: true, cancelable: true}) workflowChanged: EventEmitter<WorkflowModel>
  @State() workflowModel: WorkflowModel
  el: HTMLElement
  canvasElement: HTMLElement;
  parentActivityId?: string;
  parentActivityOutcome?: string;
  activityDisplayContexts: Map<ActivityDesignDisplayContext> = {};

  @Watch('model')
  handleModelChanged(newValue: WorkflowModel) {
    this.workflowModel = newValue;
  }

  @Listen('edit-activity')
  handleEditActivity(e: CustomEvent<ActivityModel>) {
    this.showActivityEditor(e.detail, true);
  }

  @Listen('remove-activity')
  handleRemoveActivity(e: CustomEvent<ActivityModel>) {
    this.removeActivity(e.detail.activityId);
  }

  componentWillLoad() {
    this.workflowModel = this.model;

    eventBus.on(EventTypes.ActivityPicked, async args => {
      const activityDescriptor = (args as ActivityDescriptor);
      const activityModel = this.addActivity(activityDescriptor, this.parentActivityId, null, this.parentActivityOutcome);

      if (activityDescriptor.properties.length > 0)
        this.showActivityEditor(activityModel, false);
    });

    eventBus.on(EventTypes.UpdateActivity, async args => {
      const activityModel = (args as ActivityModel);
      this.updateActivity(activityModel);
    });
  }

  componentWillRender() {
    destroy();

    const activityModels = this.workflowModel.activities;
    const displayContexts: Map<ActivityDesignDisplayContext> = {};

    for (const model of activityModels) {
      const displayContext: ActivityDesignDisplayContext = {
        activityModel: model,
        activityIcon: <ActivityIcon/>,
        bodyDisplay: <p>{model.description}</p>,
        outcomes: [...model.outcomes]
      };

      eventBus.emit(EventTypes.ActivityDesignDisplaying, this, displayContext);
      displayContexts[model.activityId] = displayContext;
    }

    this.activityDisplayContexts = displayContexts;
  }

  componentDidRender() {
    const canvasElement = this.canvasElement;
    const connections = this.getJsPlumbConnections();
    const sourceEndpoints = this.getJsPlumbSourceEndpoints();
    const targets = this.getJsPlumbTargets();

    updateConnections(
      canvasElement,
      connections,
      sourceEndpoints,
      targets,
      connection => this.onConnectionCreated(connection),
      connection => this.onConnectionDetached(connection));
  }

  disconnectedCallback() {
    cleanup();
  }

  showActivityPicker() {
    eventBus.emit(EventTypes.ShowActivityPicker);
  }

  showActivityEditor(activity: ActivityModel, animate: boolean) {
    eventBus.emit(EventTypes.ShowActivityEditor, this, activity, animate);
  }

  getRootActivities(): Array<ActivityModel> {
    return getChildActivities(this.workflowModel, null);
  }

  addActivity(activityDescriptor: ActivityDescriptor, sourceActivityId?: string, targetActivityId?: string, outcome?: string): ActivityModel {

    outcome = outcome || 'Done';

    const activity: ActivityModel =
      {
        activityId: uuid(),
        type: activityDescriptor.type,
        outcomes: activityDescriptor.outcomes,
        displayName: activityDescriptor.displayName,
        properties: []
      };

    for (const property of activityDescriptor.properties) {
      activity.properties[property.name] = {
        syntax: '',
        expression: ''
      }
    }

    const workflowModel = {...this.workflowModel, activities: [...this.workflowModel.activities, activity]};

    if (targetActivityId) {
      const existingConnection = workflowModel.connections.find(x => x.targetId == targetActivityId && x.outcome == outcome);

      if (existingConnection) {
        workflowModel.connections = workflowModel.connections.filter(x => x != existingConnection);

        const replacementConnection = {
          ...existingConnection,
          sourceId: activity.activityId
        };

        workflowModel.connections.push(replacementConnection);
      } else {
        workflowModel.connections.push({sourceId: activity.activityId, targetId: targetActivityId, outcome: outcome});
      }
    }

    if (sourceActivityId != null) {
      const existingConnection = workflowModel.connections.find(x => x.sourceId == sourceActivityId && x.outcome == outcome);

      if (existingConnection != null) {
        workflowModel.connections = workflowModel.connections.filter(x => x != existingConnection);

        const replacementConnection = {
          ...existingConnection,
          targetId: activity.activityId
        }

        workflowModel.connections.push(replacementConnection);
        const connection: ConnectionModel = {sourceId: activity.activityId, targetId: existingConnection.targetId, outcome};
        workflowModel.connections.push(connection);
      } else {
        const connection: ConnectionModel = {sourceId: sourceActivityId, targetId: activity.activityId, outcome: outcome};
        workflowModel.connections.push(connection);
      }
    }

    this.updateWorkflowModel(workflowModel);
    return activity;
  }

  removeActivity(activityId: string) {
    let workflowModel = {...this.workflowModel};
    const incomingConnections = getInboundConnections(workflowModel, activityId);
    const outgoingConnections = getOutboundConnections(workflowModel, activityId);

    // Remove activity (will also remove its connections).
    workflowModel = removeActivity(workflowModel, activityId);

    // For each incoming activity, try to connect it to a outgoing activity based on outcome.
    for (const incomingConnection of incomingConnections) {
      const incomingActivity = findActivity(workflowModel, incomingConnection.sourceId);
      const outgoingConnection = outgoingConnections.find(x => x.outcome === incomingConnection.outcome);

      if (outgoingConnection)
        workflowModel = addConnection(workflowModel, incomingActivity.activityId, outgoingConnection.targetId, incomingConnection.outcome);
    }

    this.updateWorkflowModel(workflowModel);
  }

  updateActivity(activity: ActivityModel) {
    let workflowModel = {...this.workflowModel};
    const activities = [...workflowModel.activities];
    const index = activities.findIndex(x => x.activityId === activity.activityId);
    activities[index] = activity;
    this.updateWorkflowModel({...workflowModel, activities: activities});
  }

  updateWorkflowModel(model: WorkflowModel) {
    this.workflowModel = model;
    this.workflowChanged.emit(model);
  }

  getJsPlumbConnections(): Array<any> {
    const rootActivities = getChildActivities(this.workflowModel, null);

    const rootConnections = rootActivities.flatMap(x => {
      return [
        {
          sourceId: `start-button`,
          sourceActivityId: undefined,
          targetId: `start-button-plus-${x.activityId}`,
          targetActivityId: x.activityId,
          outcome: undefined
        },
        {
          sourceId: `start-button-plus-${x.activityId}`,
          sourceActivityId: undefined,
          targetId: `activity-${x.activityId}`,
          targetActivityId: x.activityId,
          outcome: undefined
        }]
    });

    const sourceConnections = this.workflowModel.activities.flatMap(activity => {
      const displayContext = this.activityDisplayContexts[activity.activityId];
      return displayContext.outcomes.map(x =>
        ({
          sourceId: `activity-${activity.activityId}`,
          sourceActivityId: activity.activityId,
          targetId: `${activity.activityId}-${x}`,
          targetActivityId: undefined,
          outcome: x
        }));
    });

    const connections = this.workflowModel.connections.flatMap(x => [
      {
        sourceId: `${x.sourceId}-${x.outcome}`,
        sourceActivityId: x.sourceId,
        targetId: `activity-${x.targetId}`,
        targetActivityId: x.targetId,
        outcome: x.outcome
      }]
    );

    return [...rootConnections, ...connections, ...sourceConnections];
  }

  getJsPlumbSourceEndpoints(): Array<any> {
    const rootActivities = getChildActivities(this.workflowModel, null);

    const rootSourceEndpoints = rootActivities.map(x =>
      ({
        sourceId: `start-button-plus-${x.activityId}`,
        sourceActivityId: x.activityId,
        outcome: undefined
      }));

    const otherSourceEndpoints = this.workflowModel.activities.flatMap(activity => {
      const displayContext = this.activityDisplayContexts[activity.activityId];

      return displayContext.outcomes.map(x =>
        ({
          sourceId: `${activity.activityId}-${x}`,
          sourceActivityId: activity.activityId,
          outcome: x
        }));
    });

    return [...rootSourceEndpoints, ...otherSourceEndpoints];
  }

  getJsPlumbTargets() {
    return this.workflowModel.activities.map(x =>
      ({
          targetId: `activity-${x.activityId}`,
          targetActivityId: x.activityId
        }
      ));
  }

  onConnectionCreated(connection: ConnectionModel) {
    const workflowModel = {...this.workflowModel};
    workflowModel.connections = [...workflowModel.connections, connection];

    this.updateWorkflowModel(workflowModel);
  }

  onConnectionDetached(connection: ConnectionModel) {
    const workflowModel = {...this.workflowModel};
    workflowModel.connections = workflowModel.connections.filter(x => !(x.sourceId == connection.sourceId && x.targetId == connection.targetId && x.outcome == connection.outcome));

    this.updateWorkflowModel(workflowModel);
  }

  onAddButtonClick() {
    this.showActivityPicker();
  }

  onOutcomeButtonClick(e: Event, outcome?: string, activityId?: string) {
    e.preventDefault();
    this.parentActivityId = activityId;
    this.parentActivityOutcome = outcome;
    this.showActivityPicker();
  }

  async onDragStartActivity(e: DragEvent, activityDisplayContext: ActivityDesignDisplayContext) {
    const activityModel = activityDisplayContext.activityModel;
    e.dataTransfer.setData("text", activityModel.activityId);
    e.dataTransfer.effectAllowed = 'move';
  }

  onDragOverOutcome(e: DragEvent, outcome: string, sourceActivityId: string) {
    e.preventDefault();
  }

  onDropOnOutcome(e: DragEvent, outcome: string, sourceActivityId) {
    const targetActivityId = e.dataTransfer.getData('text');

    if (sourceActivityId === targetActivityId)
      return;

    e.preventDefault();

    const workflowModel = {...this.workflowModel};
    const targetActivityDisplayContext = this.activityDisplayContexts[targetActivityId];
    let connections = [...workflowModel.connections];

    // Remove current connection where dropped activity is target.
    connections = connections.filter(x => x.targetId !== targetActivityId)

    // Check if there is already a connection.
    const existingConnection = connections.find(x => x.sourceId == sourceActivityId && x.outcome == outcome);

    if (existingConnection) {
      // Try to find a matching outcome on the dropped activity to attach the existing activity to.
      const newOutcome = targetActivityDisplayContext.outcomes.find(x => x == existingConnection.outcome) || targetActivityDisplayContext.outcomes[0];

      existingConnection.sourceId = targetActivityId;
      existingConnection.outcome = newOutcome;

      // Create new connection.
      const newConnection: ConnectionModel = {
        sourceId: sourceActivityId,
        targetId: targetActivityId,
        outcome: outcome
      };

      workflowModel.connections = [...connections, newConnection];
    }
    else
    {
      // Create new connection.
      const newConnection: ConnectionModel = {
        sourceId: sourceActivityId,
        targetId: targetActivityId,
        outcome: outcome
      };

      workflowModel.connections = [...connections, newConnection];
    }

    this.updateWorkflowModel(workflowModel);
  }

  render() {
    const renderedActivities = new Set<string>();

    return (
      <Host class="workflow-canvas flex-1 flex" ref={el => this.el = el}>
        <div class="flex-1 text-gray-200">
          <div class="p-10">
            <div class="canvas select-none" ref={el => this.canvasElement = el}>
              <div class="tree">
                <ul>
                  <li>
                    <div class="inline-flex flex flex-col items-center">
                      <button id="start-button"
                              onClick={() => this.onAddButtonClick()}
                              type="button"
                              class="px-6 py-3 border border-transparent text-base leading-6 font-medium rounded-md text-white bg-green-600 hover:bg-green-500 focus:outline-none focus:border-green-700 focus:shadow-outline-green active:bg-green-700 transition ease-in-out duration-150">
                        Start
                      </button>
                    </div>
                    {this.renderTree(this.getRootActivities(), true, renderedActivities)}
                  </li>
                </ul>
              </div>
            </div>
          </div>
        </div>
      </Host>
    );
  }

  renderTree(activities: Array<ActivityModel>, isRoot: boolean, renderedActivities: Set<string>): any {
    const list = activities.filter(x => !renderedActivities.has(x.activityId));
    const cssClass = isRoot ? "root" : undefined;

    for (const activity of list)
      renderedActivities.add(activity.activityId);

    if (list.length == 0)
      return null;

    return (
      <ul class={cssClass}>
        {activities.map(x => {
          const activityId = x.activityId;
          const children = getChildActivities(this.workflowModel, activityId);
          const displayContext = this.activityDisplayContexts[activityId];

          return <li key={x.activityId}>
            <div class="inline-flex flex flex-col items-center">
              {isRoot ? this.renderOutcomeButton(`start-button-plus-${x.activityId}`, '', activityId, e => this.onOutcomeButtonClick(e, null, null)) : undefined}
              {this.renderActivity(displayContext)}
              {this.renderOutcomeButtons(displayContext)}
            </div>
            {children.length > 0 ? this.renderTree(children, false, renderedActivities) : undefined}
          </li>;
        })}
      </ul>
    );
  }

  renderOutcomeButtons(displayContext: ActivityDesignDisplayContext) {
    const activityId = displayContext.activityModel.activityId;

    return (
      <div class="flex flex-row space-x-6">
        {displayContext.outcomes.map(x => this.renderOutcomeButton(`${activityId}-${x}`, x, activityId, e => this.onOutcomeButtonClick(e, x, activityId)))}
      </div>
    );
  }

  renderOutcomeButton(id: string, outcome: string, activityId: string, clickHandler: (e: MouseEvent) => void) {
    return (
      <div class="my-6 flex flex-col items-center">
        {outcome && outcome.length > 0 ? <div
          class="mb-4 relative z-10 px-2.5 py-0.5 rounded-full text-xs font-medium leading-4 bg-cool-gray-100 text-cool-gray-800 capitalize">{outcome}</div> : undefined}
        <a key={id} id={id} href="#" onClick={e => clickHandler(e)}
           onDragOver={e => this.onDragOverOutcome(e, outcome, activityId)}
           onDrop={e => this.onDropOnOutcome(e, outcome, activityId)}
        >
          <svg class="h-8 w-8 text-gray-400 hover:text-blue-500 cursor-pointer" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v3m0 0v3m0-3h3m-3 0H9m12 0a9 9 0 11-18 0 9 9 0 0118 0z"/>
          </svg>
        </a>
      </div>
    );
  }

  renderActivity(displayContext: ActivityDesignDisplayContext) {
    return <elsa-designer-tree-activity displayContext={displayContext} draggable={true}
                                        onDragStart={e => this.onDragStartActivity(e, displayContext)}
    />;
  }
}
