import 'reflect-metadata';
import {Component, Element, Event, EventEmitter, h, Method, Prop, Watch} from '@stencil/core';
import {Edge, Graph, Model, Node, NodeView, Point} from '@antv/x6';
import {v4 as uuid} from 'uuid';
import {first} from 'lodash';
import './shapes';
import './ports';
import {ActivityNode as ActivityNodeShape} from './shapes';
import {ContainerActivityComponent} from '../container-activity-component';
import {AddActivityArgs, UpdateActivityArgs} from '../../designer/canvas/canvas';
import {Activity, ActivitySelectedArgs, ContainerSelectedArgs, GraphUpdatedArgs} from '../../../models';
import {createGraph} from './graph-factory';
import {Connection, Flowchart} from './models';
import {NodeFactory} from "./node-factory";
import {Container} from "typedi";
import {EventBus} from "../../../services";
import {ConnectionCreatedEventArgs, FlowchartEvents} from "./events";
import PositionEventArgs = NodeView.PositionEventArgs;
import FromJSONData = Model.FromJSONData;
import {ContextMenuAnchorPoint, MenuItemGroup} from "../../shared/context-menu/models";
import PointLike = Point.PointLike;
import descriptorsStore from "../../../data/descriptors-store";

@Component({
  tag: 'elsa-flowchart',
  styleUrl: 'flowchart.scss',
})
export class FlowchartComponent implements ContainerActivityComponent {
  private readonly eventBus: EventBus;
  private readonly nodeFactory: NodeFactory;
  private silent: boolean = false; // Whether to emit events or not.
  private activityContextMenu: HTMLElsaContextMenuElement;
  private activity: Flowchart;

  constructor() {
    this.eventBus = Container.get(EventBus);
    this.nodeFactory = Container.get(NodeFactory);
  }

  @Prop() interactiveMode: boolean = true;

  @Element() el: HTMLElement;
  container: HTMLElement;
  graph: Graph;
  target: Node;

  @Event() activitySelected: EventEmitter<ActivitySelectedArgs>;
  @Event() containerSelected: EventEmitter<ContainerSelectedArgs>;
  @Event() graphUpdated: EventEmitter<GraphUpdatedArgs>;

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
  async addActivity(args: AddActivityArgs): Promise<void> {
    const graph = this.graph;
    const {descriptor, x, y} = args;
    let id = args.id ?? uuid();
    const pageToLocal = graph.pageToLocal(x, y);
    const point: PointLike = pageToLocal;
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
    graph.addNode(node);
  }

  @Method()
  async updateActivity(args: UpdateActivityArgs) {
    const nodeId = args.id;
    const activity = args.activity;
    const node = this.graph.getNodes().find(x => x.id == nodeId) as any;

    // Update the node's data with the activity.
    node.data = activity;

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

  async componentDidLoad() {
    await this.createAndInitializeGraph();
  }

  render() {

    return (
      <div class="relative">
        <div
          class="absolute left-0 top-0 right-0 bottom-0"
          ref={el => this.container = el}>
        </div>
        <elsa-context-menu ref={el => this.activityContextMenu = el}
                           hideButton={true}
                           anchorPoint={ContextMenuAnchorPoint.TopLeft}
                           class="absolute"/>
      </div>
    );
  }

  disableEvents = () => this.silent = true;

  enableEvents = async (emitWorkflowChanged: boolean): Promise<void> => {
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
    graph.on('node:removed', this.onGraphChanged);
    graph.on('edge:added', this.onGraphChanged);
    graph.on('edge:removed', this.onGraphChanged);
    graph.on('edge:connected', this.onGraphChanged);

    await this.updateLayout();
  }

  private exportInternal = (): Activity => {
    const graph = this.graph;
    const graphModel = graph.toJSON();
    const activities = graphModel.cells.filter(x => x.shape == 'activity').map(x => x.data as Activity);
    const connections = graphModel.cells.filter(x => x.shape == 'elsa-edge' && !!x.data).map(x => x.data as Connection);

    let rootActivities = activities.filter(activity => {
      const hasInboundConnections = connections.find(c => c.target == activity.id) != null;
      return !hasInboundConnections;
    });

    const startActivity = rootActivities.find(x => x.canStartWorkflow) || first(rootActivities);

    const flowchart: Flowchart = this.activity ?? {
      typeName: 'Elsa.Flowchart',
      id: null,
    } as Flowchart;

    flowchart.activities = activities;
    flowchart.connections = connections;
    flowchart.start = startActivity?.id;
    flowchart.metadata = {...flowchart.metadata};
    flowchart.applicationProperties = {};
    flowchart.variables = [];

    return flowchart;
  }

  private importInternal = async (root: Activity) => {
    const descriptors = descriptorsStore.activityDescriptors;
    const flowchart = root as Flowchart;
    const activities = flowchart.activities;
    const connections = flowchart.connections;
    let edges: Array<Edge.Metadata> = [];

    this.activity = flowchart;

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

  createEdge = (connection: Connection): Edge.Metadata => {
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

  syncEdgeData = (cachedActivityId: string, updatedActivity: Activity) => {
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

  @Watch('interactiveMode')
  async onInteractiveModeChange(value: boolean) {
    const graph = this.graph;

    if (!value) {
      graph.disableSelectionMovable();
      graph.disableKeyboard();
    } else {
      graph.enableSelectionMovable();
      graph.enableKeyboard();
    }
  }

  onGraphClick = async (e: PositionEventArgs<JQuery.ClickEvent>) => this.containerSelected.emit({});

  onNodeClick = async (e: PositionEventArgs<JQuery.ClickEvent>) => {
    const node = e.node as ActivityNodeShape;
    const activity = node.data as Activity;
    const activityId = activity.id;

    const args: ActivitySelectedArgs = {
      activity: activity,
      applyChanges: async a => await this.updateActivity({id: activityId, activity: a}),
      deleteActivity: a => node.remove({deep: true})
    };

    this.activitySelected.emit(args);
  };

  onNodeContextMenu = async (e: PositionEventArgs<JQuery.ContextMenuEvent>) => {
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

  onNodeMoved = (e: PositionEventArgs<JQuery.ClickEvent>) => {

    console.debug("Node moved...");

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

  onEdgeConnected = async (e: { isNew: boolean, edge: Edge }) => {
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

  onGraphChanged = async () => {
    if (this.silent)
      return;
    this.graphUpdated.emit({exportGraph: this.exportInternal});
  }

  onToggleCanStartWorkflowClicked = (node: ActivityNodeShape) => {
    const activity = node.data as Activity;
    activity.canStartWorkflow = !activity.canStartWorkflow;
    node.activity = {...activity};
    this.onGraphChanged().then(_ => {
    });
  };

  onDeleteActivityClicked = (node: ActivityNodeShape) => {
    let cells = this.graph.getSelectedCells();

    if (cells.length == 0)
      cells = [node];

    this.graph.removeCells(cells);
  };

  onCopyActivityClicked = (node: ActivityNodeShape) => {
    let cells = this.graph.getSelectedCells();

    if (cells.length == 0)
      cells = [node];

    this.graph.copy(cells);
  };

  onCutActivityClicked = (node: ActivityNodeShape) => {
    let cells = this.graph.getSelectedCells();

    if (cells.length == 0)
      cells = [node];

    this.graph.cut(cells);
  };
}
