import 'reflect-metadata';
import {Component, Element, Event, EventEmitter, h, Listen, Method, Prop, State, Watch} from '@stencil/core';
import {camelCase, first} from 'lodash';
import {Edge, Graph, Model, Node, NodeView, Point} from '@antv/x6';
import {v4 as uuid} from 'uuid';
import './shapes';
import './ports';
import {ActivityNode, ActivityNode as ActivityNodeShape} from './shapes';
import {ContainerActivityComponent} from '../container-activity-component';
import {AddActivityArgs, UpdateActivityArgs} from '../../designer/canvas/canvas';
import {Activity, ActivityDeletedArgs, ActivityDescriptor, ActivitySelectedArgs, Container as ContainerActivity, ContainerSelectedArgs, EditChildActivityArgs, GraphUpdatedArgs} from '../../../models';
import {createGraph} from './graph-factory';
import {Connection, Flowchart, FlowchartModel, FlowchartNavigationItem} from './models';
import {NodeFactory} from "./node-factory";
import {Container} from "typedi";
import {createActivityMap, EventBus, flatten, flattenList, PortProviderRegistry, walkActivities} from "../../../services";
import {ConnectionCreatedEventArgs, FlowchartEvents} from "./events";
import {ContextMenuAnchorPoint, MenuItemGroup} from "../../shared/context-menu/models";
import descriptorsStore from "../../../data/descriptors-store";
import {WorkflowNavigationItem} from "../../designer/workflow-navigator/models";
import {Hash} from "../../../utils";
import PositionEventArgs = NodeView.PositionEventArgs;
import FromJSONData = Model.FromJSONData;
import PointLike = Point.PointLike;
import FlowchartTunnel, {FlowchartState} from "./state";

@Component({
  tag: 'elsa-flowchart',
  styleUrl: 'flowchart.scss',
})
export class FlowchartComponent implements ContainerActivityComponent {
  private readonly eventBus: EventBus;
  private readonly nodeFactory: NodeFactory;
  private readonly portProviderRegistry: PortProviderRegistry;
  private silent: boolean = false; // Whether to emit events or not.
  private activityContextMenu: HTMLElsaContextMenuElement;
  private activity: Flowchart;
  private nodeMap: Hash<Activity> = {};

  constructor() {
    this.eventBus = Container.get(EventBus);
    this.nodeFactory = Container.get(NodeFactory);
    this.portProviderRegistry = Container.get(PortProviderRegistry);
  }

  @Prop() interactiveMode: boolean = true;

  @Element() el: HTMLElement;
  container: HTMLElement;
  graph: Graph;
  target: Node;

  @Event() activitySelected: EventEmitter<ActivitySelectedArgs>;
  @Event() activityDeleted: EventEmitter<ActivityDeletedArgs>;
  @Event() containerSelected: EventEmitter<ContainerSelectedArgs>;
  @Event() graphUpdated: EventEmitter<GraphUpdatedArgs>;

  @State() private currentPath: Array<FlowchartNavigationItem> = [];

  @Method()
  async getGraph(): Promise<Graph> {
    return this.graph;
  }

  @Method()
  async reset(): Promise<void> {

    const model: FromJSONData = {nodes: [], edges: []};

    // Freeze then unfreeze prevents an error from occurring when importing JSON a second time (e.g. after loading a new workflow.
    this.graph.freeze();
    this.graph.fromJSON(model, {silent: false});
    this.graph.unfreeze();
    this.activity = null;
  }

  @Method()
  async updateLayout(): Promise<void> {
    const width = this.el.clientWidth;
    const height = this.el.clientHeight;
    this.graph.resize(width, height);
    this.graph.updateBackground();
  }

  @Method()
  async zoomToFit() {
    const graph = this.graph;
    graph.zoomToFit();
  }

  @Method()
  async addActivity(args: AddActivityArgs): Promise<Activity> {
    const graph = this.graph;
    const {descriptor, x, y} = args;
    let id = args.id ?? uuid();
    const point: PointLike = graph.pageToLocal(x, y);
    const sx = point.x;
    const sy = point.y;

    const activity: Activity = {
      id: id,
      typeName: descriptor.activityType,
      applicationProperties: {},
      metadata: {
        designer: {
          position: {
            x: sx,
            y: sy
          }
        }
      },
    };

    const node = this.nodeFactory.createNode(descriptor, activity, sx, sy);
    graph.addNode(node, {merge: true});
    return activity;
  }

  @Method()
  async updateActivity(args: UpdateActivityArgs) {
    const nodeId = args.id;
    const activity = args.activity;
    const node = this.graph.getNodes().find(x => x.id == nodeId) as ActivityNode;

    if (!node)
      return;

    // Update the node's data with the activity.
    node.setData(activity, {overwrite: true});

    // Updating the node's activity property to trigger a rerender.
    node.activity = activity;

    // If the ID of the activity changed, we need to update connection references (X6 stores deep copies of data).
    if (activity.id !== nodeId)
      this.syncEdgeData(nodeId, activity);
  }

  @Method()
  async export(): Promise<Activity> {
    return this.exportInternal();
  }

  @Method()
  async import(root: Activity): Promise<void> {
    return this.importInternal(root);
  }

  @Method()
  async getCurrentLevel(): Promise<Activity> {
    return this.getCurrentLevelInternal();
  }

  async componentDidLoad() {
    await this.createAndInitializeGraph();
  }

  @Listen('editChildActivity')
  private async editChildActivity(e: CustomEvent<EditChildActivityArgs>) {
    const parentActivityId = e.detail.parentActivityId;
    const currentActivityId = this.currentPath[this.currentPath.length - 1].activityId;
    const currentActivity = this.nodeMap[currentActivityId];
    const parentActivity = this.nodeMap[parentActivityId] as Flowchart;
    const parentActivityDescriptor = descriptorsStore.activityDescriptors.find(x => x.activityType == parentActivity.typeName);
    const indexInParent = currentActivity.activities?.findIndex(x => x == parentActivity);
    const portName = e.detail.port.name;

    const item: WorkflowNavigationItem = {
      activityId: parentActivityId,
      portName: portName,
      index: indexInParent
    };

    const portProvider = this.portProviderRegistry.get(parentActivity.typeName);
    let activityProperty = portProvider.resolvePort(portName, {activity: parentActivity, activityDescriptor: parentActivityDescriptor}) as Flowchart;

    if (!activityProperty) {
      activityProperty = this.createContainer();
      portProvider.assignPort(portName, activityProperty, {activity: parentActivity, activityDescriptor: parentActivityDescriptor});
    }

    const isContainer = Array.isArray(activityProperty);

    if (isContainer) {
      await this.setupGraph(parentActivity);
    } else {
      await this.setupGraph(activityProperty);
    }

    this.currentPath = [...this.currentPath, item];
  }

  private createContainer = (): Flowchart => {
    return {
      typeName: 'Elsa.Flowchart',
      id: null,
      start: null,
      activities: [],
      connections: [],
      metadata: {},
      variables: [],
      applicationProperties: {},
      canStartWorkflow: false
    };
  }

  private disableEvents = () => this.silent = true;

  private enableEvents = async (emitWorkflowChanged: boolean): Promise<void> => {
    this.silent = false;

    if (emitWorkflowChanged === true) {
      await this.onGraphChanged();
    }
  };

  private createAndInitializeGraph = async () => {
    const graph = this.graph = createGraph(this.container, {
        nodeMovable: () => this.interactiveMode,
        edgeMovable: () => this.interactiveMode,
        arrowheadMovable: () => this.interactiveMode,
        edgeLabelMovable: () => this.interactiveMode,
        magnetConnectable: () => this.interactiveMode,
        useEdgeTools: () => this.interactiveMode,
        toolsAddable: () => this.interactiveMode,
        stopDelegateOnDragging: () => this.interactiveMode,
        vertexAddable: () => this.interactiveMode,
        vertexDeletable: () => this.interactiveMode,
        vertexMovable: () => this.interactiveMode,
      },
      this.disableEvents,
      this.enableEvents);

    graph.on('blank:click', this.onGraphClick);
    graph.on('node:click', this.onNodeClick);
    graph.on('node:contextmenu', this.onNodeContextMenu);
    graph.on('edge:connected', this.onEdgeConnected);
    graph.on('node:moved', this.onNodeMoved);

    graph.on('node:change:*', this.onGraphChanged);
    graph.on('node:added', this.onGraphChanged);
    graph.on('node:removed', this.onNodeRemoved);
    graph.on('node:removed', this.onGraphChanged);
    graph.on('edge:added', this.onGraphChanged);
    graph.on('edge:removed', this.onGraphChanged);
    graph.on('edge:connected', this.onGraphChanged);

    await this.updateLayout();
  }

  private updateModel = () => {
    debugger;
    const model = this.exportModel();
    let currentLevel = this.getCurrentLevelInternal();

    if (!currentLevel) {
      currentLevel = this.createContainer();
    }

    currentLevel.activities = model.activities;
    currentLevel.connections = model.connections;
    currentLevel.start = model.start;

    const currentPath = this.currentPath;
    const currentNavigationItem = currentPath[currentPath.length - 1];
    const currentPortName = currentNavigationItem?.portName;
    const currentContainer = this.nodeMap[currentNavigationItem.activityId] as Activity;
    const currentContainerDescriptor = descriptorsStore.activityDescriptors.find(x => x.activityType == currentContainer.typeName);

    if (!!currentPortName) {
      // if (currentActivityDescriptor.isContainer) {
      //   const parentNavigationItem = currentPath[currentPath.length - 2];
      //   const parentActivityId = parentNavigationItem.activityId;
      //   const parentActivity = this.nodeMap[parentActivityId] as ContainerActivity;
      //   const parentActivityDescriptor = descriptorsStore.activityDescriptors.find(x => x.activityType == parentActivity.typeName);
      //   const portProvider = this.portProviderRegistry.get(parentActivity.typeName);
      //   const parentActivitiesProp = portProvider.resolvePort(currentPortName, {activity: parentActivity, activityDescriptor: parentActivityDescriptor}) as Array<Activity>;
      //   parentActivitiesProp[currentNavigationItem.index] = currentLevel;
      // } else {
      const portProvider = this.portProviderRegistry.get(currentContainer.typeName);
      portProvider.assignPort(currentPortName, currentLevel, {activity: currentContainer, activityDescriptor: currentContainerDescriptor});
      //}
    } else {
      this.activity = currentLevel;
    }

    const activityNodes = flatten(walkActivities(this.activity));
    this.nodeMap = createActivityMap(activityNodes);
    //this.currentPath = [{activityId: this.activity.id, portName: null, index: 0}];
  }

  private exportInternal = (): Activity => {
    const model = this.exportModel();
    const currentLevel = this.getCurrentLevelInternal();
    const currentPath = this.currentPath;
    const currentNavigationItem = currentPath[currentPath.length - 1];
    const currentPortName = currentNavigationItem?.portName;
    const currentActivityDescriptor = descriptorsStore.activityDescriptors.find(x => x.activityType == this.activity.typeName);

    if (!!currentPortName) {
      if (currentActivityDescriptor.isContainer) {
        const parentNavigationItem = currentPath[currentPath.length - 2];
        const parentActivityId = parentNavigationItem.activityId;
        const parentActivity = this.nodeMap[parentActivityId] as ContainerActivity;
        const parentActivityDescriptor = descriptorsStore.activityDescriptors.find(x => x.activityType == parentActivity.typeName);
        const portProvider = this.portProviderRegistry.get(parentActivity.typeName);
        const parentActivitiesProp = portProvider.resolvePort(currentPortName, {activity: parentActivity, activityDescriptor: parentActivityDescriptor}) as Array<Activity>;
        parentActivitiesProp[currentNavigationItem.index] = currentLevel;
      } else {
        const portProvider = this.portProviderRegistry.get(currentLevel.typeName);
        portProvider.assignPort(currentPortName, currentLevel, {activity: currentLevel, activityDescriptor: currentActivityDescriptor});
      }
    } else {
      this.activity = currentLevel;
    }

    const activityNodes = flatten(walkActivities(this.activity));
    this.nodeMap = createActivityMap(activityNodes);
    this.currentPath = [{activityId: this.activity.id, portName: null, index: 0}];

    return this.activity;
  }

  private importInternal = async (root: Activity) => {
    debugger;
    const flowchart = root as Flowchart;
    const activityNodes = flatten(walkActivities(flowchart));

    this.activity = flowchart;
    this.nodeMap = createActivityMap(activityNodes);
    this.currentPath = [{activityId: flowchart.id, portName: null, index: 0}];

    this.setupGraph(flowchart);
  };

  private setupGraph = (flowchart: Flowchart) => {
    const descriptors = descriptorsStore.activityDescriptors;
    const activities = flowchart.activities;
    const connections = flowchart.connections;
    const edges: Array<Edge.Metadata> = [];

    // Create an X6 node for each activity.
    const nodes: Array<Node.Metadata> = activities.map(activity => {
      const position = activity.metadata.designer?.position || {x: 100, y: 100};
      const {x, y} = position;
      const descriptor = descriptors.find(x => x.activityType == activity.typeName)
      return this.nodeFactory.createNode(descriptor, activity, x, y);
    });

    // Create X6 edges for each connection in the flowchart.
    for (const connection of connections) {
      const edge: Edge.Metadata = this.createEdge(connection);
      edges.push(edge);
    }

    const model: FromJSONData = {nodes, edges};

    // Freeze then unfreeze prevents an error from occurring when importing JSON a second time (e.g. after loading a new workflow.
    this.graph.freeze();
    this.graph.fromJSON(model, {silent: false});
    this.graph.unfreeze();
  };

  private exportModel = (): FlowchartModel => {
    const graph = this.graph;
    const graphModel = graph.toJSON();
    const activities = graphModel.cells.filter(x => x.shape == 'activity').map(x => x.data as Activity);
    const connections = graphModel.cells.filter(x => x.shape == 'elsa-edge' && !!x.data).map(x => x.data as Connection);

    let rootActivities = activities.filter(activity => {
      const hasInboundConnections = connections.find(c => c.target == activity.id) != null;
      return !hasInboundConnections;
    });

    const startActivity = rootActivities.find(x => x.canStartWorkflow) || first(rootActivities);

    return {
      activities,
      connections,
      start: startActivity?.id
    };
  };

  private createEdge = (connection: Connection): Edge.Metadata => {
    return {
      shape: 'elsa-edge',
      zIndex: -1,
      data: connection,
      source: connection.source,
      target: connection.target,
      sourcePort: connection.sourcePort,
      targetPort: connection.targetPort
    };
  }

  private syncEdgeData = (cachedActivityId: string, updatedActivity: Activity) => {
    const graph = this.graph;
    const edges = graph.model.getEdges().filter(x => x.shape == 'elsa-edge' && !!x.data);

    for (const edge of edges) {
      const connection: Connection = edge.data;

      if (connection.target != cachedActivityId && connection.source != cachedActivityId)
        continue;

      if (connection.target == cachedActivityId)
        connection.target = updatedActivity.id;

      if (connection.source == cachedActivityId)
        connection.source = updatedActivity.id;

      edge.data = connection;
    }
  };

  private getCurrentLevelInternal(): Flowchart {
    debugger;
    const currentItem = this.currentPath.length > 0 ? this.currentPath[this.currentPath.length - 1] : null;

    if (!currentItem)
      return this.activity;

    const activity = this.nodeMap[currentItem.activityId] as Flowchart;
    const activityDescriptor = descriptorsStore.activityDescriptors.find(x => x.activityType == activity.typeName);

    if (activityDescriptor.isContainer)
      return activity;

    const portProvider = this.portProviderRegistry.get(activity.typeName);
    return portProvider.resolvePort(currentItem.portName, {activity, activityDescriptor}) as Flowchart;
  }

  private generateUniqueActivityName = async (activityDescriptor: ActivityDescriptor): Promise<string> => {
    const workflowDefinition = this.workflowDefinitionState;
    const root = workflowDefinition.root;
    const graph = walkActivities(root);
    const activityNodes = flattenList(graph.children);
    return await generateUniqueActivityName(activityNodes, activityDescriptor);
  };

  @Watch('interactiveMode')
  private async onInteractiveModeChange(value: boolean) {
    const graph = this.graph;

    if (!value) {
      graph.disableSelectionMovable();
      graph.disableKeyboard();
    } else {
      graph.enableSelectionMovable();
      graph.enableKeyboard();
    }
  }

  private onGraphClick = async (e: PositionEventArgs<JQuery.ClickEvent>) => {
    const activityId = this.activity?.id;

    const args: ContainerSelectedArgs = {
      activity: this.activity,
    };
    return this.containerSelected.emit(args);
  };

  private onNodeClick = async (e: PositionEventArgs<JQuery.ClickEvent>) => {
    const node = e.node as ActivityNodeShape;
    const activity = node.data as Activity;

    const args: ActivitySelectedArgs = {
      activity: activity,
    };

    this.activitySelected.emit(args);
  };

  private onNodeContextMenu = async (e: PositionEventArgs<JQuery.ContextMenuEvent>) => {
    const node = e.node as ActivityNodeShape;
    const activity = e.node.data as Activity;

    const menuItemGroups: Array<MenuItemGroup> = [
      {
        menuItems: [{
          text: 'Startable',
          clickHandler: () => this.onToggleCanStartWorkflowClicked(node),
          isToggle: true,
          checked: activity.canStartWorkflow,
        }]
      }, {
        menuItems: [
          {
            text: 'Cut',
            clickHandler: () => this.onCutActivityClicked(node)
          },
          {
            text: 'Copy',
            clickHandler: () => this.onCopyActivityClicked(node)
          }]
      }, {
        menuItems: [{
          text: 'Delete',
          clickHandler: () => this.onDeleteActivityClicked(node)
        }]
      }];

    this.activityContextMenu.menuItemGroups = menuItemGroups;
    const localPos = this.graph.localToClient(e.x, e.y);
    this.activityContextMenu.style.top = `${localPos.y}px`;
    this.activityContextMenu.style.left = `${localPos.x}px`;

    await this.activityContextMenu.open();
  }

  private onNodeMoved = (e: PositionEventArgs<JQuery.ClickEvent>) => {
    const node = e.node as ActivityNodeShape;
    const activity = node.data as Activity;
    const nodePosition = node.position({relative: false});

    activity.metadata = {
      ...activity.metadata,
      designer: {
        ...activity.metadata.designer,
        position: {
          x: nodePosition.x,
          y: nodePosition.y
        }
      }
    }
  }

  private onEdgeConnected = async (e: { isNew: boolean, edge: Edge }) => {
    const edge = e.edge;
    const sourceNode = edge.getSourceNode();
    const targetNode = edge.getTargetNode();
    const sourceActivity = edge.getSourceNode().data as Activity;
    const targetActivity = targetNode.data as Activity;
    const sourcePort = sourceNode.getPort(edge.getSourcePortId()).id;
    const targetPort = targetNode.getPort(edge.getTargetPortId()).id;

    const connection: Connection = {
      source: sourceActivity.id,
      sourcePort: sourcePort,
      target: targetActivity.id,
      targetPort: targetPort
    };

    edge.data = connection;

    const eventArgs: ConnectionCreatedEventArgs = {
      graph: this.graph,
      connection,
      sourceNode,
      targetNode,
      sourceActivity,
      targetActivity,
      edge
    }

    await this.eventBus.emit(FlowchartEvents.ConnectionCreated, this, eventArgs);
  }

  private onGraphChanged = async () => {
    if (this.silent) {
      return;
    }
    this.updateModel();
    //this.graphUpdated.emit({exportGraph: this.exportInternal});
  }

  private onNodeRemoved = (e: any) => {
    const activity = e.node.data as Activity;
    this.activityDeleted.emit({activity});
  };

  private onToggleCanStartWorkflowClicked = (node: ActivityNodeShape) => {
    const activity = node.data as Activity;
    activity.canStartWorkflow = !activity.canStartWorkflow;
    node.activity = {...activity};
    this.onGraphChanged().then(_ => {
    });
  };

  private onDeleteActivityClicked = (node: ActivityNodeShape) => {
    let cells = this.graph.getSelectedCells();

    if (cells.length == 0)
      cells = [node];

    this.graph.removeCells(cells);

    for (const cell of cells) {
      const activity = node.data as Activity;
      this.activityDeleted.emit({activity: activity});
    }
  };

  private onCopyActivityClicked = (node: ActivityNodeShape) => {
    let cells = this.graph.getSelectedCells();

    if (cells.length == 0)
      cells = [node];

    this.graph.copy(cells);
  };

  private onCutActivityClicked = (node: ActivityNodeShape) => {
    let cells = this.graph.getSelectedCells();

    if (cells.length == 0)
      cells = [node];

    this.graph.cut(cells);

    for (const cell of cells) {
      const activity = node.data as Activity;
      this.activityDeleted.emit({activity: activity});
    }
  };

  private onNavigateHierarchy = async (e: CustomEvent<WorkflowNavigationItem>) => {
    const item = e.detail;
    const activityId = item.activityId;
    let activity = this.nodeMap[activityId];
    const activityDescriptor = descriptorsStore.activityDescriptors.find(x => x.activityType == activity.typeName);
    const path = this.currentPath;
    const index = path.indexOf(item);

    this.currentPath = path.slice(0, index + 1);

    if (!!item.portName) {
      if (!activityDescriptor.isContainer) {
        const portName = camelCase(item.portName);
        activity = activity[portName] as Activity;
      }
    }

    await this.importInternal(activity);
  }

  render() {
    const activity = this.activity;

    const state: FlowchartState = {
      flowchart: activity,
      nodeMap: this.nodeMap
    }

    return (
      <FlowchartTunnel.Provider state={state}>
        <div class="relative">
          <div class="absolute left-0 top-3 z-10">
            <elsa-workflow-navigator-2 items={this.currentPath} flowchart={activity} onNavigate={this.onNavigateHierarchy}/>
          </div>
          <div
            class="absolute left-0 top-0 right-0 bottom-0"
            ref={el => this.container = el}>
          </div>
          <elsa-context-menu ref={el => this.activityContextMenu = el}
                             hideButton={true}
                             anchorPoint={ContextMenuAnchorPoint.TopLeft}
                             class="absolute"/>
        </div>
      </FlowchartTunnel.Provider>
    );
  }
}
