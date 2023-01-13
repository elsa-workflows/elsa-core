import 'reflect-metadata';
import {Component, Element, Event, EventEmitter, h, Listen, Method, Prop, State, Watch} from '@stencil/core';
import {camelCase, first} from 'lodash';
import {Edge, Graph, Model, Node, NodeView, Point} from '@antv/x6';
import './shapes';
import './ports';
import {ActivityNodeShape} from './shapes';
import {Activity, ActivityDeletedArgs, ActivityDescriptor, ActivitySelectedArgs, ContainerSelectedArgs, EditChildActivityArgs, GraphUpdatedArgs, Port, WorkflowUpdatedArgs} from '../../models';
import {createGraph} from './graph-factory';
import {AddActivityArgs, Connection, Flowchart, FlowchartModel, FlowchartNavigationItem, FlowchartPathItem, LayoutDirection, RenameActivityArgs, UpdateActivityArgs} from './models';
import {NodeFactory} from "./node-factory";
import {Container} from "typedi";
import {ActivityNode, createActivityLookup, EventBus, flatten, PortProviderRegistry, walkActivities} from "../../services";
import {ConnectionCreatedEventArgs, FlowchartEvents} from "./events";
import {ContextMenuAnchorPoint, ContextMenuItem} from "../../components/shared/context-menu/models";
import descriptorsStore from "../../data/descriptors-store";
import {Hash} from "../../utils";
import PositionEventArgs = NodeView.PositionEventArgs;
import FromJSONData = Model.FromJSONData;
import PointLike = Point.PointLike;
import {generateUniqueActivityName} from "../../utils/generate-activity-name";
import {DagreLayout, OutNode} from '@antv/layout';
import {adjustPortMarkupByNode, rebuildGraph} from '../../utils/graph';
import {WorkflowDefinition} from "../workflow-definitions/models/entities";
import FlowchartTunnel, {FlowchartState} from "./state";

const FlowchartTypeName = 'Elsa.Flowchart';

@Component({
  tag: 'elsa-flowchart',
  styleUrl: 'flowchart.scss',
})
export class FlowchartComponent {
  private readonly eventBus: EventBus;
  private readonly nodeFactory: NodeFactory;
  private readonly portProviderRegistry: PortProviderRegistry;
  private activityContextMenu: HTMLElsaContextMenuElement;

  constructor() {
    this.eventBus = Container.get(EventBus);
    this.nodeFactory = Container.get(NodeFactory);
    this.portProviderRegistry = Container.get(PortProviderRegistry);
  }

  @Prop() workflowDefinition: WorkflowDefinition;
  @Prop() interactiveMode: boolean = true;
  @Prop() silent: boolean = false;

  @Element() el: HTMLElement;
  container: HTMLElement;
  graph: Graph;
  target: Node;

  @Event() activitySelected: EventEmitter<ActivitySelectedArgs>;
  @Event() activityDeleted: EventEmitter<ActivityDeletedArgs>;
  @Event() containerSelected: EventEmitter<ContainerSelectedArgs>;
  @Event() graphUpdated: EventEmitter<GraphUpdatedArgs>;
  @Event() workflowUpdated: EventEmitter<WorkflowUpdatedArgs>;

  @State() private activityLookup: Hash<Activity> = {};
  @State() private activities: Array<Activity> = [];
  @State() private path: Array<FlowchartPathItem> = [];

  @Method()
  async newRoot(): Promise<Activity> {
    const flowchart = await this.createFlowchart();
    await this.setupGraph(flowchart);
    return flowchart;
  }

  @Method()
  async updateGraph() {
    const currentFlowchart = await this.getCurrentFlowchartActivity();
    await this.setupGraph(currentFlowchart);
  }

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
  async scrollToStart() {
    const flowchartModel = this.getFlowchartModel();
    const startActivity = flowchartModel.activities.find(x => x.id == flowchartModel.start);
    if (startActivity != null) {
      this.graph.scrollToCell(this.graph.getCells()[0]);
    }
  }

  @Method()
  async autoLayout(direction: LayoutDirection) {
    const dagreLayout = new DagreLayout({
      type: 'dagre',
      rankdir: direction,
      align: 'UL',
      ranksep: 30,
      nodesep: 15,
      controlPoints: true,
    });

    const flowchartModel = this.getFlowchartModel();
    const nodes = [];
    const edges = [];

    flowchartModel.activities.forEach(activity => {
      const activityElement = document.querySelectorAll("[activity-id=\"" + activity.id + "\"]")[0].getBoundingClientRect();
      nodes.push({id: activity.id, size: {width: activityElement.width, height: activityElement.height}, x: activity.metadata.designer.position.x, y: activity.metadata.designer.position.y})
    });

    flowchartModel.connections.forEach((connection, index) => {
      edges.push({id: index, source: connection.source, target: connection.target});
    });

    let data = {nodes: nodes, edges: edges}
    let newLayout = dagreLayout.layout(data);

    newLayout.nodes.forEach(node => {
      let outNode = node as OutNode;
      let activity = flowchartModel.activities.find(x => x.id == node.id);
      activity.metadata.designer.position.x = outNode.x;
      activity.metadata.designer.position.y = outNode.y;

      this.updateActivity({id: activity.id, originalId: activity.id, activity: activity});
    });

    this.updateGraphInternal(flowchartModel.activities, flowchartModel.connections);
    this.graphUpdated.emit({});
  }

  @Method()
  async addActivity(args: AddActivityArgs): Promise<Activity> {
    const graph = this.graph;
    const {descriptor, x, y} = args;
    let id = args.id ?? await this.generateUniqueActivityName(descriptor);
    const point: PointLike = graph.pageToLocal(x, y);
    const sx = point.x;
    const sy = point.y;

    const activity: Activity = {
      id: id,
      type: descriptor.typeName,
      version: descriptor.version,
      customProperties: {},
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

    adjustPortMarkupByNode(graph.getNodes().find(n => n.id == node.id));
    await this.updateModel();
    return activity;
  }

  @Method()
  async updateActivity(args: UpdateActivityArgs) {
    const activityId = args.id;
    const originalId = args.originalId;
    const nodeId = originalId;
    const activity = args.activity;
    const node = this.graph.getNodes().find(x => x.id == nodeId) as ActivityNodeShape;

    if (!!node) {

      // Update the node's data with the activity.
      node.setData(activity, {overwrite: true});

      // Updating the node's activity property to trigger a rerender.
      node.activity = activity;

      // If the ID of the activity changed, we need to update connection references (X6 stores deep copies of data).
      if (activityId !== originalId)
        this.syncEdgeData(nodeId, activity);
    }

    this.updateLookups();
  }

  @Method()
  public async renameActivity(args: RenameActivityArgs) {
    const nodeId = args.originalId;
    const activity = args.activity;
    const node = this.graph.getNodes().find(x => x.id == nodeId) as ActivityNodeShape;

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

  @Watch('workflowDefinition')
  private onWorkflowDefinitionChanged(value: WorkflowDefinition) {
    this.updateLookups();
  }

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

  @Listen('editChildActivity')
  private async editChildActivity(e: CustomEvent<EditChildActivityArgs>) {
    const portName = e.detail.port.name;
    const activityId = e.detail.parentActivityId;
    const item: FlowchartPathItem = {activityId, portName};

    // Push child prop path.
    this.path = [...this.path, item];

    // Get current child.
    const currentActivity = await this.getCurrentActivity();

    // Get flowchart of child.
    let childFlowchart = await this.getCurrentFlowchartActivity();

    // If there's no flowchart, create it.
    if (!childFlowchart) {
      childFlowchart = await this.createFlowchart();
      this.setPort(currentActivity, portName, childFlowchart);
    }

    await this.setupGraph(childFlowchart);
  }

  async componentWillLoad() {
    this.updateLookups();
  }

  async componentDidLoad() {
    await this.createAndInitializeGraph();
  }

  private getFlowchartDescriptor = () => this.getActivityDescriptor(FlowchartTypeName);

  private createFlowchart = async (): Promise<Flowchart> => {
    const descriptor = this.getFlowchartDescriptor();
    const activityId = await this.generateUniqueActivityName(descriptor);

    return {
      type: descriptor.typeName,
      version: descriptor.version,
      id: activityId,
      start: null,
      activities: [],
      connections: [],
      metadata: {},
      variables: [],
      customProperties: {},
      canStartWorkflow: false
    };
  }

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
      this.getAllActivities);

    graph.on('blank:click', this.onGraphClick);
    graph.on('node:click', this.onNodeClick);
    graph.on('node:contextmenu', this.onNodeContextMenu);
    graph.on('edge:connected', this.onEdgeConnected);
    graph.on('node:moved', this.onNodeMoved);

    //graph.on('node:change:*', this.onGraphChanged);
    graph.on('node:moved', this.onGraphChanged);
    graph.on('node:added', this.onGraphChanged);
    graph.on('node:removed', this.onNodeRemoved);
    graph.on('node:removed', this.onGraphChanged);
    graph.on('edge:added', this.onGraphChanged);
    graph.on('edge:removed', this.onGraphChanged);
    graph.on('edge:connected', this.onGraphChanged);

    await this.updateLayout();
  }

  private getCurrentActivity = (): Activity => {
    const activityLookup = this.activityLookup;
    const path = this.path;

    if (path.length > 0) {
      const lastItem = path[path.length - 1];
      return activityLookup[lastItem.activityId];
    }

    return this.workflowDefinition.root;
  };

  private getCurrentFlowchartActivity = async (): Promise<Flowchart> => {
    const path = this.path;
    let currentActivity = this.getCurrentActivity();

    if (path.length > 0) {
      const lastItem = path[path.length - 1];
      return this.resolvePort(currentActivity, lastItem.portName);
    }

    return currentActivity as Flowchart;
  };

  private resolvePort = (activity: Activity, portName: string): Flowchart => {
    const portProvider = this.portProviderRegistry.get(activity.type);
    const activityDescriptor = descriptorsStore.activityDescriptors.find(x => x.typeName == activity.type);
    const childActivity = portProvider.resolvePort(portName, {activity: activity, activityDescriptor});
    return childActivity as Flowchart;
  };

  private setPort = (owner: Activity, portName: string, child: Flowchart) => {
    const portProvider = this.portProviderRegistry.get(owner.type);
    const activityDescriptor = descriptorsStore.activityDescriptors.find(x => x.typeName == owner.type);
    portProvider.assignPort(portName, child, {activity: owner, activityDescriptor});
    this.updateLookups();
  }

  private updateModel = async () => {
    const model = this.getFlowchartModel();
    const currentFlowchart = await this.getCurrentFlowchartActivity();

    currentFlowchart.activities = model.activities;
    currentFlowchart.connections = model.connections;
    currentFlowchart.start = model.start;
    this.updateLookups();
  }

  private exportInternal = (): Activity => {
    return this.workflowDefinition.root;
  }

  private getActivityDescriptor = (typeName: string): ActivityDescriptor => descriptorsStore.activityDescriptors.find(x => x.typeName == typeName)

  private setupGraph = async (flowchart: Flowchart) => {
    const activities = flowchart.activities;
    const connections = flowchart.connections;
    this.updateGraphInternal(activities, connections);
    await this.scrollToStart();
  };

  private updateGraphInternal = (activities: Array<Activity>, connections: Array<Connection>) => {
    const edges: Array<Edge.Metadata> = [];

    // Create an X6 node for each activity.
    const nodes: Array<Node.Metadata> = activities.map(activity => {
      const position = activity.metadata.designer?.position || {x: 100, y: 100};
      const {x, y} = position;
      const descriptor = this.getActivityDescriptor(activity.type);
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

    rebuildGraph(this.graph);
  }

  private getFlowchartModel = (): FlowchartModel => {
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

  private updateLookups = () => {
    const activityNodes = flatten(walkActivities(this.workflowDefinition.root));
    this.activities = activityNodes.map(x => x.activity);
    this.activityLookup = createActivityLookup(activityNodes);
  }

  private generateUniqueActivityName = async (activityDescriptor: ActivityDescriptor): Promise<string> => {
    return await generateUniqueActivityName(this.activities, activityDescriptor);
  };

  private getAllActivities = (): Array<Activity> => this.activities;

  private onGraphClick = async (e: PositionEventArgs<JQuery.ClickEvent>) => {
    const currentFlowchart = await this.getCurrentFlowchartActivity();

    const args: ContainerSelectedArgs = {
      activity: currentFlowchart
    };

    return this.containerSelected.emit(args);
  };

  private onNodeClick = async (e: PositionEventArgs<JQuery.ClickEvent>) => {
    const node = e.node as ActivityNodeShape;
    const activityCopy = node.activity;

    // X6 nodes store a copy of the data, so we need to get the original activity from the workflow definition.
    const activity = this.activityLookup[activityCopy.id];

    const args: ActivitySelectedArgs = {
      activity: activity,
    };

    this.activitySelected.emit(args);
  };

  private onNodeContextMenu = async (e: PositionEventArgs<JQuery.ContextMenuEvent>) => {
    const node = e.node as ActivityNodeShape;
    const activity = e.node.data as Activity;

    const menuItems: Array<ContextMenuItem> = [
      {
        text: 'Startable',
        handler: () => this.onToggleCanStartWorkflowClicked(node),
        isToggle: true,
        checked: activity.canStartWorkflow,

      }, {
        text: 'Cut',
        handler: () => this.onCutActivityClicked(node)
      },
      {
        text: 'Copy',
        handler: () => this.onCopyActivityClicked(node)

      }, {
        text: 'Delete',
        handler: () => this.onDeleteActivityClicked(node)
      }];

    this.activityContextMenu.menuItems = menuItems;
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

  private onGraphChanged = async (e: any) => {
    await this.updateModel();
    this.graphUpdated.emit();
  }

  private onNodeRemoved = (e: any) => {
    const activity = e.node.data as Activity;
    this.activityDeleted.emit({activity});
  };

  private onToggleCanStartWorkflowClicked = (node: ActivityNodeShape) => {
    const activity = node.data as Activity;
    activity.canStartWorkflow = !activity.canStartWorkflow;
    node.activity = {...activity};
    // this.onGraphChanged().then(_ => {
    // });
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

  private onNavigateHierarchy = async (e: CustomEvent<FlowchartPathItem>) => {
    const item = e.detail;
    const path = this.path;
    const index = path.indexOf(item);

    this.path = path.slice(0, index + 1);
    const childFlowchart = await this.getCurrentFlowchartActivity();

    await this.setupGraph(childFlowchart);
  }

  render() {

    const path = this.path;

    const state: FlowchartState = {
      nodeMap: this.activityLookup
    }

    return (
      <FlowchartTunnel.Provider state={state}>
        <div class="relative">
          <div class="absolute left-0 top-3 z-10">
            <elsa-workflow-navigator items={path} workflowDefinition={this.workflowDefinition} onNavigate={this.onNavigateHierarchy}/>
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
