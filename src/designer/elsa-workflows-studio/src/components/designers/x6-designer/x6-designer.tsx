import {Component, Event, Element, EventEmitter, h, Host, Method, Prop, State, Watch} from '@stencil/core';
import {v4 as uuid} from 'uuid';
import {
  addConnection,
  findActivity,
  getChildActivities,
  getInboundConnections,
  getOutboundConnections,
  Map,
  removeActivity,
  removeConnection
} from '../../../utils/utils';
import {
  ActivityDescriptor,
  ActivityDesignDisplayContext,
  ActivityModel,
  ActivityTraits,
  ConnectionModel,
  EventTypes,
  WorkflowModel,
  WorkflowPersistenceBehavior,
  ActivityDeletedArgs,
} from '../../../models';
import {
  eventBus
} from '../../../services';
import state from '../../../utils/store';
import {ActivityIcon} from '../../icons/activity-icon';
import {ActivityContextMenuState, LayoutDirection, WorkflowDesignerMode} from "../tree/elsa-designer-tree/models";
import {createGraph, addGraphEvents, removeGraphEvents} from './graph-factory';
import {Edge, Graph, Model, Node, Point} from '@antv/x6';
import  {ActivityNodeShape} from "./shapes";
import dagre from "dagre";

@Component({
  tag: 'x6-designer',
  styleUrls: ['x6-designer.css'],
  assetsDirs: ['assets'],
  shadow: false,
})
export class ElsaWorkflowDesigner {
  container: HTMLElement;
  graph: Graph;
  @Element() el: HTMLElement;
  parentActivityId?: string;
  parentActivityOutcome?: string;
  addingActivity: boolean = false;
  selectedActivities: Map<ActivityModel> = {};
  ignoreCopyPasteActivities: boolean = false;
  silent: boolean = false;
  ignoreNextNodeSelect: boolean = false;
  containerObserver: ResizeObserver;
  activityDisplayContexts: Map<ActivityDesignDisplayContext> = null;
  workflowSaveTimer?: NodeJS.Timer = null;
  edgeUpdateTimer?: NodeJS.Timer = null;

  @Prop() model: WorkflowModel = {
    activities: [],
    connections: [],
    persistenceBehavior: WorkflowPersistenceBehavior.WorkflowBurst
  };

  @Prop() selectedActivityIds: Array<string> = [];
  @Prop() activityContextMenuButton?: (activity: ActivityModel) => string;
  @Prop() activityBorderColor?: (activity: ActivityModel) => string;
  @Prop() activityContextMenu?: ActivityContextMenuState;
  @Prop() connectionContextMenu?: ActivityContextMenuState;
  @Prop() activityContextTestMenu?: ActivityContextMenuState;
  @Prop() mode: WorkflowDesignerMode = WorkflowDesignerMode.Edit;
  @Prop() layoutDirection: LayoutDirection = LayoutDirection.TopBottom;
  @Prop({attribute: 'enable-multiple-connections'}) enableMultipleConnectionsFromSingleSource: boolean;
  @Event({
    eventName: 'workflow-changed',
    bubbles: true,
    composed: true,
    cancelable: true
  }) workflowChanged: EventEmitter<WorkflowModel>;
  @Event() activitySelected: EventEmitter<ActivityModel>;
  @Event() activityDeselected: EventEmitter<ActivityModel>;
  @Event() activityContextMenuButtonClicked: EventEmitter<ActivityContextMenuState>;
  @Event() connectionContextMenuButtonClicked: EventEmitter<ActivityContextMenuState>;
  @Event() activityContextMenuButtonTestClicked: EventEmitter<ActivityContextMenuState>;
  @State() workflowModel: WorkflowModel;

  @State() activityContextMenuState: ActivityContextMenuState = {
    shown: false,
    x: 0,
    y: 0,
    activity: null,
    selectedActivities: {}
  };

  @State() connectionContextMenuState: ActivityContextMenuState = {
    shown: false,
    x: 0,
    y: 0,
    activity: null,
  };

  @State() activityContextMenuTestState: ActivityContextMenuState = {
    shown: false,
    x: 0,
    y: 0,
    activity: null,
  };

  @Watch('activityContextMenu')
  handleActivityContextMenuChanged(newValue: ActivityContextMenuState) {
    this.activityContextMenuState = newValue;
  }

  @Watch('connectionContextMenu')
  handleConnectionContextMenuChanged(newValue: ActivityContextMenuState) {
    this.connectionContextMenuState = newValue;
  }

  @Watch('activityContextTestMenu')
  handleActivityContextMenuTestChanged(newValue: ActivityContextMenuState) {
    this.activityContextMenuTestState = newValue;
  }

  @Watch('mode')
  handleActivityContextMenuButtonChanged(newValue: WorkflowDesignerMode) {
    if (this.mode !== WorkflowDesignerMode.Edit) {
      this.graph.resetSelection();
      this.graph.disableRubberband();
      this.graph.disableHistory();
      removeGraphEvents(this.graph);
    } else {
      this.graph.enableRubberband();
      this.graph.enableHistory();
      addGraphEvents(this.graph, this.disableEvents, this.enableEvents, false);
    }
    setTimeout(() => this.updateGraph(), 1);
  }

  @Watch('model')
  handleModelChanged(newValue: WorkflowModel) {
    if(this.model !== newValue){
      this.updateWorkflowModel(newValue, false);
      setTimeout(() => this.updateGraph(), 1);
    }
  }

  @Method()
  async removeActivity(activity: ActivityModel) {
    const cell = this.graph.getCells().find(x => x.id === activity.activityId);
    if (cell) {
      this.graph.removeCell(cell);
    }
  }

  @Method()
  async removeSelectedActivities() {
    const cells = this.graph.getCells().filter(x => this.selectedActivityIds.includes(x.id));
    this.graph.removeCells(cells);
  }

  @Method()
  async showActivityEditor(activity: ActivityModel, animate: boolean) {
    await this.showActivityEditorInternal(activity, animate);
  }

  @Event() activityDeleted: EventEmitter<ActivityDeletedArgs>;

  @Method()
  async updateLayout(): Promise<void> {
    const width = this.el.clientWidth;
    const height = this.el.clientHeight;
    this.graph.resize(width, height);
    this.graph.updateBackground();
  }

  async componentDidLoad() {
    await this.createAndInitializeGraph();

    // Below fixes issues, that arise when navigating from another page to this one - the graph not being fully loaded in the DOM at the correct time
    const containerObserver = new ResizeObserver(() => {
      if (this.container.clientHeight > 0) {
        this.onContainerLoaded();
        containerObserver.unobserve(this.container);
      }
    });
    containerObserver.observe(this.container);
  }

  private onContainerLoaded() {
    if (this.isAutoLayoutRequired) {
      setTimeout(() => this.applyAutoLayout(), 100);
    }
    this.updateGraph();
    if (this.workflowModel.activities.length) {
      setTimeout(() => this.graph.scrollToContent(), 10);
    }
  }

  private createAndInitializeGraph = async () => {
    const graph = this.graph = createGraph(this.container,
      {},
      this.disableEvents,
      this.enableEvents,
      this.mode !== WorkflowDesignerMode.Edit);

    graph.on('blank:click', this.onGraphClick);
    graph.on('node:selected', this.onNodeSelected);
    graph.on('node:unselected', this.onNodeUnselected);
    graph.on('node:contextmenu', this.onNodeContextMenu);
    graph.on('edge:connected', this.onEdgeConnected);
    graph.on('node:moved', this.onNodeMoved);

    graph.on('node:removed', this.updateModelFromGraph);
    graph.on('node:change:*', this.updateModelFromGraph);
    graph.on('node:added', this.updateModelFromGraph);
    graph.on('edge:added', this.updateModelFromGraph);
    graph.on('edge:removed', this.updateModelFromGraph);
    graph.on('edge:connected', this.updateModelFromGraph);

    await this.updateLayout();
  }

  private disableEvents = () => this.silent = true;

  private enableEvents = (emitWorkflowChanged: boolean) => {
    this.silent = false;

    if (emitWorkflowChanged === true) {
      this.updateModelFromGraph();
    }
  };

  applyAutoLayout() {
    const graph = new dagre.graphlib.Graph();
    graph.setGraph({ rankdir: "TB", nodesep: 30, ranksep: 180 });

    graph.setDefaultEdgeLabel(() => ({}));

    this.workflowModel.activities.forEach(activity => {
      const activityElement = document.querySelectorAll("g[data-cell-id=\"" + activity.activityId + "\"]")[0].getBoundingClientRect();
      graph.setNode(activity.activityId, {
        label: activity.activityId,
        width: Math.max(220, activityElement.width),
        height: Math.max(64, activityElement.height)
      });
    });

    this.workflowModel.connections.forEach(connection => {
      graph.setEdge(connection.sourceId, connection.targetId);
    });

    dagre.layout(graph);

    this.disableEvents();

    this.graph.batchUpdate(() => {
      this.workflowModel.activities.forEach(activity => {
        const node = graph.node(activity.activityId);
        const cell = this.graph.getCellById(activity.activityId);

        cell.setProp('position', { x: node.x, y: node.y });

        activity.x = Math.round(node.x);
        activity.y = Math.round(node.y);

        (cell as any).activity = activity;
      });
    });

    this.graph.scrollToContent();
    this.enableEvents(true);

    setTimeout(() => this.updateGraphEdgeViews(), 100);

    console.log("Auto-layout applied");
  };
  connectedCallback() {
    eventBus.on(EventTypes.WorkflowImported, this.onWorkflowImported);
    eventBus.on(EventTypes.ActivityPicked, this.onActivityPicked);
    eventBus.on(EventTypes.UpdateActivity, this.onUpdateActivityExternal);
    //eventBus.on(EventTypes.PasteActivity, this.onPasteActivity);
    // eventBus.on(EventTypes.HideModalDialog, this.onCopyPasteActivityEnabled);
    // eventBus.on(EventTypes.ShowWorkflowSettings, this.onCopyPasteActivityDisabled);
    eventBus.on(EventTypes.TestActivityMessageReceived, this.updateGraph);
  }

  disconnectedCallback() {
    eventBus.detach(EventTypes.WorkflowImported, this.onWorkflowImported);
    eventBus.detach(EventTypes.ActivityPicked, this.onActivityPicked);
    eventBus.detach(EventTypes.UpdateActivity, this.onUpdateActivityExternal);
    //eventBus.detach(EventTypes.PasteActivity, this.onPasteActivity);
    // eventBus.detach(EventTypes.HideModalDialog, this.onCopyPasteActivityEnabled);
    // eventBus.detach(EventTypes.ShowWorkflowSettings, this.onCopyPasteActivityDisabled);
    eventBus.detach(EventTypes.TestActivityMessageReceived, this.updateGraph);
  }

  componentWillLoad() {
    this.workflowModel = this.model;
  }

  async componentWillRender() {
    if (!!this.activityDisplayContexts)
      return;
    const activityModels = this.workflowModel.activities;
    const displayContexts: Map<ActivityDesignDisplayContext> = {};

    for (const model of activityModels)
      displayContexts[model.activityId] = await this.getActivityDisplayContext(model);

    this.activityDisplayContexts = displayContexts;
  }

  get isAutoLayoutRequired(): boolean {
    if (this.workflowModel.activities.length < 2) {
      return false;
    }
    return this.workflowModel.activities.findIndex(x => !!x.x || !!x.y) === -1;
  }

  handleContextMenuChange(state: ActivityContextMenuState) {
    this.ignoreCopyPasteActivities = true;
    this.activityContextMenuState = state;
    this.activityContextMenuButtonClicked.emit(state);
  }

  handleConnectionContextMenuChange(state: ActivityContextMenuState) {
    this.connectionContextMenuState = state;
    this.connectionContextMenuButtonClicked.emit(state);
  }

  handleContextMenuTestChange(state: ActivityContextMenuState) {
    this.ignoreCopyPasteActivities = true;
    this.activityContextMenuTestState = state;
    this.activityContextMenuButtonTestClicked.emit(state);
  }

  getOutcomes = (activity: ActivityModel, definition: ActivityDescriptor): Array<string> => {
    let outcomes = [];
    const displayContext = this.activityDisplayContexts[activity.activityId];

    if (!!definition) {
      const lambda = displayContext.outcomes || definition.outcomes;

      if (lambda instanceof Array) {
        outcomes = lambda as Array<string>;
      } else {
        const value = eval(lambda);

        if (value instanceof Array)
          outcomes = value;

        else if (value instanceof Function) {
          try {
            outcomes = value({ activity, definition, state: activity.state });
          } catch (e) {
            console.warn(e);
            outcomes = [];
          }
        }
      }
    }

    return !!outcomes ? outcomes : [];
  }

  private onNodeMoved = async (e) => {
    const node = e.node as ActivityNodeShape;
    const activity = node.activity as ActivityModel;
    const nodePosition = node.position({relative: false});

    activity.x = Math.round(nodePosition.x);
    activity.y = Math.round(nodePosition.y);

    if(node.id !== activity.activityId) activity.activityId = node.id;

    this.updateActivityInternal(activity);
  }

  private onNodeSelected = async (e) => {
    if (this.ignoreNextNodeSelect) {
      this.ignoreNextNodeSelect = false;
      return;
    }
    const node = e.node as ActivityNodeShape;
    const activity = node.activity as ActivityModel;

    this.selectedActivities[activity.activityId] = activity;
    this.activitySelected.emit(activity);
  };

  private onNodeUnselected = async (e) => {
    const node = e.node as ActivityNodeShape;
    const activity = node.activity as ActivityModel;

    if (!!this.selectedActivities[activity.activityId]) {
      this.activityDeselected.emit(this.selectedActivities[activity.activityId]);
      delete this.selectedActivities[activity.activityId];
    }
  };

  private onNodeContextMenu = (e) => {
    e.e.preventDefault();
    const node = e.node as ActivityNodeShape;

    if (this.mode == WorkflowDesignerMode.Edit || this.mode == WorkflowDesignerMode.Instance) {
      this.handleContextMenuChange({
        x: e.e.clientX,
        y: e.e.clientY,
        shown: true,
        activity: node.activity,
        selectedActivities: this.selectedActivities
      });
    }
  }

  private onGraphClick = async () => {
    for (const key in this.selectedActivities) {
      this.activityDeselected.emit(this.selectedActivities[key]);
    }
    this.selectedActivities = {};
  };

  async onAddActivity(e: Event) {
    e.preventDefault();
    if (this.mode !== WorkflowDesignerMode.Test) await this.showActivityPicker();
  }

  private onEdgeConnected = async (e: { edge: Edge }) => {
    const edge = e.edge;
    let workflowModel = {...this.workflowModel};

    const sourceNode = edge.getSourceNode();
    const targetId = edge.getTargetCellId();
    const sourcePort = edge.getSourcePortId();

    edge.data = this.addConnection(sourceNode.id, targetId, sourcePort);

    if (!this.enableMultipleConnectionsFromSingleSource) {
      const existingConnection = workflowModel.connections.find(x => x.sourceId === sourceNode.id && x.targetId == targetId);

      if (!existingConnection) {
        // Add created connection to list.
        const newConnection: ConnectionModel = {
          sourceId: sourceNode.id,
          targetId: targetId,
          outcome: sourcePort
        };
        edge.data = newConnection;

        edge.insertLabel(sourcePort);

        workflowModel.connections = [...workflowModel.connections, newConnection];
        this.updateWorkflowModel(workflowModel);
      }
  }}

  private createGraphNode = (item: ActivityModel): Node.Metadata => {
    const desciptors: Array<ActivityDescriptor> = state.activityDescriptors;
    const descriptor = desciptors.find(x => x.type == item.type);

    const outcomes: Array<string> = this.mode === WorkflowDesignerMode.Blueprint ? item.outcomes : this.getOutcomes(item, descriptor);
    let ports = [{ group: 'in', id: uuid(), attrs: {} }];
    outcomes.forEach(outcome =>
      ports.push({
        id: outcome,
        group: 'out',
        attrs: {
          text: {
            text: outcome
          }
        }
      })
    );
    const node: Node.Metadata = {
      id: item.activityId,
      shape: 'activity',
      x: item.x || 0,
      y: item.y || 0,
      type: item.type,
      activity: item,
      ports: {
        items: ports
      },
      component: this.renderActivity(item)
    }
    return node;
  }

  private getEdgeStep = (sourceId: string, targetId: string) => {
    // The purpose of this function is to prevent connections from overlapping because of their target nodes being on the same height
    const targetNode = this.workflowModel.activities.find(x => x.activityId === targetId);
    const siblingEdges = this.workflowModel.connections.filter(x => x.sourceId === sourceId);
    const siblingActivities = siblingEdges.map(x => this.workflowModel.activities.find(a => a.activityId === x.targetId)).filter(x => !!x);

    let xStep = 10;
    let yStep = 10;
    if (targetNode) {
      const sameYNodes = siblingActivities
        .filter(x => Math.abs(x.y - targetNode.y) < 20)
        .sort((a, b) => a.x > b.x ? 1 : -1);
      const sameXNodes = siblingActivities
        .filter(x => Math.abs(x.x - targetNode.x) < 20)
        .sort((a, b) => a.y > b.y ? 1 : -1);

      if (sameYNodes.length > 1) {
        yStep += sameYNodes.findIndex(x => x.activityId == targetNode.activityId) * 32;
      }
      if (sameXNodes.length > 1) {
        xStep += sameXNodes.findIndex(x => x.activityId == targetNode.activityId) * 32;
      }
    }
    return Math.max(xStep, yStep);
  }

  private createEdge = (connection: ConnectionModel): Edge.Metadata => {
    return {
      shape: 'elsa-edge',
      zIndex: -1,
      data: connection,
      source: connection.sourceId,
      sourcePort: connection.outcome,
      target: connection.targetId,
      targetPort: connection.targetPort,
      outcome: connection.outcome,
      router: this.createEdgeRouter(connection.sourceId, connection.targetId),
      labels:
        [{
          attrs: {
            label: { text: connection.outcome }
          },
        }],
    };
  }

  private createEdgeRouter = (sourceId: string, targetId: string) => {
    const sourceNode = this.activityDisplayContexts[sourceId];
    return {
      name: 'manhattan',
      args: {
        padding: 10,
        step: this.getEdgeStep(sourceId, targetId),
        startDirections: (sourceNode?.outcomes.length > 3) ? ['bottom'] : ['right'],
        endDirections: ['left']
      },
    }
  }

  async getActivityDisplayContext(activityModel: ActivityModel): Promise<ActivityDesignDisplayContext> {
    const activityDescriptors: Array<ActivityDescriptor> = state.activityDescriptors;
    let descriptor = activityDescriptors.find(x => x.type == activityModel.type);

    if (!descriptor)
      descriptor = this.createNotFoundActivityDescriptor(activityModel);

    const displayContext: ActivityDesignDisplayContext = {
      activityModel: activityModel,
      activityDescriptor: descriptor,
      outcomes: [...activityModel.outcomes]
    };

    await eventBus.emit(EventTypes.ActivityDesignDisplaying, this, displayContext);

    //Remove duplicates
    displayContext.outcomes = displayContext.outcomes.filter(function(item, pos) {
      return displayContext.outcomes.indexOf(item) == pos;
    });
    return displayContext;
  }

  private updateModelFromGraph = (e?: { edge: Edge, node: Node }) => {
    if (this.silent) {
      return;
    }

    this.updateGraphEdgeViews();

    const graph = this.graph;
    const graphModel = graph.toJSON();
    const activities = graphModel.cells.filter(x => x.shape == 'activity').map(x => x.activity as ActivityModel);

    const connections = graphModel.cells
      .map(x => (e?.edge && e.edge.data && x.id == e.edge.id) ? this.createEdge(e.edge.data) : x)
      .filter(x => x.shape == 'elsa-edge' && !!x.data).map(x => x.data as ConnectionModel);

    const workflowModel = {
      activities,
      connections,
      persistenceBehavior: WorkflowPersistenceBehavior.WorkflowBurst
    };
    this.updateWorkflowModel(workflowModel);
  }

  private updateGraphEdgeViews = () => {
    // This updates the connection routing between nodes, when one node gets moved for example

    this.disableEvents();
    if (this.edgeUpdateTimer) {
      clearTimeout(this.edgeUpdateTimer);
    }
    this.edgeUpdateTimer = setTimeout(() => {
      this.edgeUpdateTimer = null;
      this.graph.disableHistory();
      const edges = this.graph.getCells().filter(x => x.shape == 'elsa-edge') as any[];
      for(const edge of edges) {
        edge.router = this.createEdgeRouter(edge.source.cell, edge.target.cell);
        (this.graph.findViewByCell(edge.id) as any).update();
      }
      this.graph.enableHistory();
    }, 10);

    this.enableEvents(false);
  }

  private updateGraph = async () => {
    const activities = this.workflowModel.activities;
    const connections = this.workflowModel.connections;
    const edges: Array<Edge.Metadata> = [];

    for (const model of activities)
      this.activityDisplayContexts[model.activityId] = await this.getActivityDisplayContext(model);

    // Create an X6 node for each activity.
    const nodes: Array<Node.Metadata> = activities.map(activity => this.createGraphNode(activity));

    // Create X6 edges for each connection in the flowchart.
    for (const connection of connections) {
      const edge: Edge.Metadata = this.createEdge(connection);
      edges.push(edge);
    }

    const model: Model.FromJSONData = {nodes, edges};

    // Freeze then unfreeze prevents an error from occurring when importing JSON a second time (e.g. after loading a new workflow.
    this.graph.freeze();
    this.graph.fromJSON(model, {silent: false});
    this.graph.unfreeze();
  }

  private findActivityById = (id: string): ActivityModel => this.workflowModel.activities.find(x => x.activityId === id);

  createNotFoundActivityDescriptor(activityModel: ActivityModel): ActivityDescriptor {
    return {
      outcomes: ['Done'],
      inputProperties: [],
      type: `(Not Found) ${activityModel.type}`,
      outputProperties: [],
      displayName: `(Not Found) ${activityModel.displayName || activityModel.name || activityModel.type}`,
      traits: ActivityTraits.Action,
      description: `(Not Found) ${activityModel.description}`,
      category: 'Not Found',
      browsable: false,
      customAttributes: {}
    };
  }

  async showActivityEditorInternal(activity: ActivityModel, animate: boolean) {
    await eventBus.emit(EventTypes.ActivityEditor.Show, this, activity, animate);
  }

  updateWorkflowModel(model: WorkflowModel, emitEvent: boolean = true) {
    if (emitEvent && this.mode !== WorkflowDesignerMode.Edit) {
      this.updateGraph();
      return;
    }
    this.workflowModel = model;

    if (emitEvent) {
      //Debounce the emitting of change event and saving of workflow
      if (this.workflowSaveTimer) {
        clearTimeout(this.workflowSaveTimer);
      }
      this.workflowSaveTimer = setTimeout(() => {
        this.workflowChanged.emit(this.workflowModel);
      }, 200);
    }
  }

  removeActivityInternal(activity: ActivityModel, model?: WorkflowModel) {
    let workflowModel = model || {...this.workflowModel};
    const incomingConnections = getInboundConnections(workflowModel, activity.activityId);
    const outgoingConnections = getOutboundConnections(workflowModel, activity.activityId);

    // Remove activity (will also remove its connections).
    workflowModel = removeActivity(workflowModel, activity.activityId);

    // For each incoming activity, try to connect it to an outgoing activity based on outcome.
    if (outgoingConnections.length > 0 && incomingConnections.length > 0) {
      for (const incomingConnection of incomingConnections) {
        const incomingActivity = findActivity(workflowModel, incomingConnection.sourceId);
        let outgoingConnection = outgoingConnections.find(x => x.outcome === incomingConnection.outcome);

        // If not matching outcome was found, pick the first one. The user will have to manually reconnect to the desired outcome.
        if (!outgoingConnection)
          outgoingConnection = outgoingConnections[0];

        if (!!outgoingConnection)
          workflowModel = addConnection(workflowModel, {
            sourceId: incomingActivity.activityId,
            targetId: outgoingConnection.targetId,
            outcome: incomingConnection.outcome
          });
      }
    }

    delete this.selectedActivities[activity.activityId];

    if (!model) {
      this.updateWorkflowModel(workflowModel);
    }

    return workflowModel;
  }

  newActivity(activityDescriptor: ActivityDescriptor): ActivityModel {
    const graphRect = this.el.getBoundingClientRect();
    const point: Point = this.graph.pageToLocal(graphRect.left + 64, graphRect.top + 64);

    const activity: ActivityModel = {
      activityId: uuid(),
      type: activityDescriptor.type,
      outcomes: activityDescriptor.outcomes,
      displayName: activityDescriptor.displayName,
      properties: [],
      propertyStorageProviders: {},
      x: Math.round(point.x),
      y: Math.round(point.y)
    };

    for (const property of activityDescriptor.inputProperties) {
      activity.properties[property.name] = {
        syntax: '',
        expression: '',
      };
    }

    return activity;
  }

  async addActivity(activity: ActivityModel, sourceActivityId?: string, targetActivityId?: string, outcome?: string) {
    outcome = outcome || 'Done';

    const workflowModel = {...this.workflowModel, activities: [...this.workflowModel.activities, activity]};

    if (targetActivityId) {
      const existingConnection = workflowModel.connections.find(x => x.targetId == targetActivityId && x.outcome == outcome);

      if (existingConnection) {
        workflowModel.connections = workflowModel.connections.filter(x => x != existingConnection);

        const replacementConnection = {
          ...existingConnection,
          sourceId: activity.activityId,
        };

        workflowModel.connections.push(replacementConnection);
      } else {
        workflowModel.connections.push({sourceId: activity.activityId, targetId: targetActivityId, outcome: outcome});
      }
    }

    if (sourceActivityId != null) {
      const existingConnection = workflowModel.connections.find(x => x.sourceId == sourceActivityId && x.outcome == outcome);

      if (existingConnection != null) {
        // Remove the existing connection
        workflowModel.connections = workflowModel.connections.filter(x => x != existingConnection);

        // Create a new outbound connection between the source and the added activity.
        const newOutboundConnection: ConnectionModel = {
          sourceId: existingConnection.sourceId,
          targetId: activity.activityId,
          outcome: existingConnection.outcome
        };

        workflowModel.connections.push(newOutboundConnection);

        // Create a new outbound activity between the added activity and the target of the source activity.
        const connection: ConnectionModel = {
          sourceId: activity.activityId,
          targetId: existingConnection.targetId,
          outcome: activity.outcomes[0]
        };

        workflowModel.connections.push(connection);
      } else {
        const connection: ConnectionModel = {
          sourceId: sourceActivityId,
          targetId: activity.activityId,
          outcome: outcome
        };
        workflowModel.connections.push(connection);
      }
    }

    this.activityDisplayContexts[activity.activityId] = await this.getActivityDisplayContext(activity);

    this.updateWorkflowModel(workflowModel);

    var newNode = this.createGraphNode(activity);
    this.graph.addNode(newNode, {merge: true});

    this.parentActivityId = null;
    this.parentActivityOutcome = null;
    this.selectActivityNode(activity);
  }

  selectActivityNode(activity: ActivityModel) {
    this.graph.resetSelection();
    const newCell = this.graph.getCells().find(x => x.id === activity.activityId);
    this.graph.select(newCell);

    for (const key in this.selectedActivities) {
      this.activityDeselected.emit(this.selectedActivities[key]);
    }

    this.selectedActivities = {};
    this.selectedActivities[activity.activityId] = activity;
    this.activitySelected.emit(activity);

    this.handleContextMenuChange({
      x: activity.x,
      y: activity.y,
      shown: true,
      activity: activity,
      selectedActivities: this.selectedActivities
    });
  }

  addConnection(sourceActivityId: string, targetActivityId: string, outcome: string): ConnectionModel {
    const workflowModel = {...this.workflowModel};
    const newConnection: ConnectionModel = {
      sourceId: sourceActivityId,
      targetId: targetActivityId,
      outcome: outcome};
    let connections = workflowModel.connections;

    if (!this.enableMultipleConnectionsFromSingleSource)
      connections = [...workflowModel.connections.filter(x => !(x.sourceId === sourceActivityId && x.outcome === outcome))];

    workflowModel.connections = [...connections, newConnection];
    this.updateWorkflowModel(workflowModel);
    this.parentActivityId = null;
    this.parentActivityOutcome = null;
    return newConnection;
  }

  updateActivityInternal(activity: ActivityModel) {
    let workflowModel = {...this.workflowModel};
    const activities = [...workflowModel.activities];
    const index = activities.findIndex(x => x.activityId === activity.activityId);
    activities[index] = activity;
    this.updateWorkflowModel({...workflowModel, activities: activities});
  }

  async updateActivityExternal(activity: ActivityModel) {
    var originalActivity = this.findActivityById(activity.activityId);
    // Do not allow external updates to change activity position, because position can only be changed internally by the graph
    activity.x = originalActivity.x;
    activity.y = originalActivity.y;

    this.graph.cleanHistory();
    this.updateActivityInternal(activity);
    this.updateGraph();
  }

  async showActivityPicker() {
    await eventBus.emit(EventTypes.ShowActivityPicker);
  }

  onWorkflowImported = args => {
    this.workflowModel = args;
    this.updateGraph();
    if (this.isAutoLayoutRequired) {
      setTimeout(() => {
        this.applyAutoLayout();
      }, 1);
    }
    this.graph.scrollToContent();
  };

  onActivityPicked = async args => {
    const activityDescriptor = args as ActivityDescriptor;
    const activityModel = this.newActivity(activityDescriptor);

    const connectFromRoot = !this.parentActivityOutcome || this.parentActivityOutcome == '';
    const sourceId = connectFromRoot ? null : this.parentActivityId;
    const targetId = connectFromRoot ? this.parentActivityId : null;
    this.addActivity(activityModel, sourceId, targetId, this.parentActivityOutcome);
  };

  onUpdateActivityExternal = args => {
    const activityModel = args as ActivityModel;

    if (this.addingActivity) {
      console.debug(`adding activity with ID ${activityModel.activityId}`)
      const connectFromRoot = !this.parentActivityOutcome || this.parentActivityOutcome == '';
      const sourceId = connectFromRoot ? null : this.parentActivityId;
      const targetId = connectFromRoot ? this.parentActivityId : null;
      this.addActivity(activityModel, sourceId, targetId, this.parentActivityOutcome);
      this.addingActivity = false;
    } else {
      console.debug(`updating activity with ID ${activityModel.activityId}`)
      this.updateActivityExternal(activityModel);
    }
  };

  handleActivityStatsClick(activity: ActivityModel) {
    if (this.mode === WorkflowDesignerMode.Test) {
      setTimeout(() => {
        const nodeEl = this.container.querySelectorAll(`div[stats-activity-id="${activity.activityId}"] button`)[0];
        nodeEl?.addEventListener("click", (evt: MouseEvent) => {
          this.ignoreNextNodeSelect = true;
          evt.stopPropagation();
          this.activityContextMenuButtonTestClicked.emit({x: evt.clientX, y: evt.clientY, shown: true, activity: activity});
        });
      }, 1);
    }
    if (this.mode === WorkflowDesignerMode.Instance) {
      setTimeout(() => {
        const nodeEl = this.container.querySelectorAll(`div[stats-activity-id="${activity.activityId}"] button`)[0];
        nodeEl?.addEventListener("click", (evt: MouseEvent) => {
          this.ignoreNextNodeSelect = true;
          evt.stopPropagation();

          this.handleContextMenuChange({
            x: evt.clientX,
            y: evt.clientY,
            shown: true,
            activity: activity,
            selectedActivities: this.selectedActivities
          });
        });
      }, 1);
    }
  }

  renderActivity(activity: ActivityModel) {
    const activityBorderColor = !!this.activityBorderColor ? this.activityBorderColor(activity) : 'gray';
    const selectedColor = !!this.activityBorderColor ? activityBorderColor : 'blue';
    const cssClass = `elsa-border-${activityBorderColor}-200 hover:elsa-border-${selectedColor}-600`;
    const typeName = activity.type;
    let activityContextMenuButton = !!this.activityContextMenuButton ? this.activityContextMenuButton(activity) : '';

    const activityDescriptors: Array<ActivityDescriptor> = state.activityDescriptors;
    let descriptor = activityDescriptors.find(x => x.type == activity.type);

    if (!descriptor) {
      descriptor = this.createNotFoundActivityDescriptor(activity);
    }

    const displayName = !!descriptor ? activity.displayName : `(Not Found) ${activity.displayName}`;
    const color = (descriptor.traits &= ActivityTraits.Trigger) == ActivityTraits.Trigger ? 'rose' : 'sky';
    const activityIcon = <ActivityIcon color={color} />;

    this.handleActivityStatsClick(activity);

    return `<div class="elsa-border-2 elsa-border-solid ${cssClass}">
      <div class="elsa-p-2" style="padding-right: 2.6rem">
        <div class="elsa-flex elsa-justify-between elsa-space-x-4 mr-4">
          <div class="elsa-flex-shrink-0">
            ${activityIcon}
          </div>
          <div class="elsa-flex-1 elsa-font-medium elsa-leading-8 elsa-overflow-hidden">
            <p class="elsa-overflow-ellipsis elsa-text-base">${displayName}</p>
            ${typeName !== displayName ? `<p class="elsa-text-gray-400 elsa-text-sm">${typeName}</p>` : ''}
          </div>
          <div class="context-menu-button-container elsa-absolute elsa-z-1" stats-activity-id="${activity.activityId}" style="right: 0.5rem">
            ${activityContextMenuButton}
          </div>
        </div>
      </div>
    </div>`;
  }

  render() {
    return (
      <Host>
        <div class="workflow-canvas elsa-flex-1 elsa-flex">
          <div ref={el => (this.container = el)}></div>
        </div>
        {this.mode == WorkflowDesignerMode.Edit &&
          <div class="start-btn elsa-absolute elsa-z-1">
            <button type="button" onClick={ e => this.onAddActivity(e)} class="elsa-h-12 elsa-px-6 elsa-mx-3 elsa-border elsa-border-transparent elsa-text-base elsa-font-medium elsa-rounded-md elsa-text-white elsa-bg-green-600 hover:elsa-bg-green-500 focus:elsa-outline-none focus:elsa-border-green-700 focus:elsa-shadow-outline-green active:elsa-bg-green-700 elsa-transition elsa-ease-in-out elsa-duration-150 elsa-top-8">Add activity</button>
          </div>
        }
        {this.mode !== WorkflowDesignerMode.Test &&
          <div class="layout-btn elsa-absolute elsa-z-1">
            <button type="button" onClick={ e => this.applyAutoLayout()} class="elsa-h-12 elsa-px-6 elsa-mx-3 elsa-border elsa-border-transparent elsa-text-base elsa-font-medium elsa-rounded-md elsa-text-white elsa-bg-yellow-600 hover:elsa-bg-yellow-500 focus:elsa-outline-none focus:elsa-border-yellow-50 focus:elsa-shadow-outline-green active:elsa-bg-yellow-500 elsa-transition elsa-ease-in-out elsa-duration-150 elsa-top-8">Auto-layout</button>
          </div>
        }
        {this.mode == WorkflowDesignerMode.Test ?
          <div>
            <div id="left" style={{border:`4px solid orange`, position:`absolute`, height: `calc(100vh - 64px)`, width:`4px`, top:`0`, bottom:`0`, left:`0`}}/>
            <div id="right" style={{ border:`4px solid orange`, position:`absolute`, height: `calc(100vh - 64px)`, width:`4px`, top:`0`, bottom:`0`, right:`0`}}/>
            <div id="top" style={{border:`4px solid orange`, position:`absolute`, height:`4px`, left:`0`, right:`0`, top:`0`}}/>
            <div id="bottom" style={{border:`4px solid orange`, position:`absolute`, height:`4px`, left:`0`, right:`0`, bottom:`0`}}/>
          </div>
          :
          undefined
        }
      </Host>
    );
  }
}
