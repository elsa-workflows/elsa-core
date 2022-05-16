import 'reflect-metadata';
import {Component, Element, Event, EventEmitter, h, Method, Prop, Watch} from '@stencil/core';
import {Edge, Graph, Model, Node, NodeView, Point} from '@antv/x6';
import {v4 as uuid} from 'uuid';
import {first} from 'lodash';
import './shapes';
import './ports';
import {ActivityNode as ActivityNodeShape} from './shapes';
import {ContainerActivityComponent} from '../container-activity-component';
import {AddActivityArgs} from '../../designer/canvas/canvas';
import {Activity, ActivityDescriptor, ActivitySelectedArgs, ContainerSelectedArgs, GraphUpdatedArgs} from '../../../models';
import {createGraph} from './graph-factory';
import {Connection, Flowchart} from './models';
import WorkflowEditorTunnel from '../../designer/state';
import {ActivityNode, flattenList, walkActivities} from "./activity-walker";
import {NodeFactory} from "./node-factory";
import {Container} from "typedi";
import {EventBus} from "../../../services";
import {ConnectionCreatedEventArgs, FlowchartEvents} from "./events";
import {TransposeHandlerRegistry} from "./transpose-handler-registry";
import PositionEventArgs = NodeView.PositionEventArgs;
import FromJSONData = Model.FromJSONData;
import {ContextMenuAnchorPoint, MenuItem, MenuItemGroup} from "../../shared/context-menu/models";
import PointLike = Point.PointLike;

@Component({
  tag: 'elsa-flowchart',
  styleUrl: 'flowchart.scss',
})
export class FlowchartComponent implements ContainerActivityComponent {
  private readonly eventBus: EventBus;
  private readonly nodeFactory: NodeFactory;
  private rootId: string = uuid();
  private silent: boolean = false; // Whether to emit events or not.
  private activityContextMenu: HTMLElsaContextMenuElement;

  constructor() {
    this.eventBus = Container.get(EventBus);
    this.nodeFactory = Container.get(NodeFactory);
  }

  @Prop({mutable: true}) public activityDescriptors: Array<ActivityDescriptor> = [];
  @Prop({mutable: true}) public root?: Activity;
  @Prop() public interactiveMode: boolean = true;

  @Element() el: HTMLElement;
  container: HTMLElement;
  graph: Graph;
  target: Node;

  @Event() activitySelected: EventEmitter<ActivitySelectedArgs>;
  @Event() containerSelected: EventEmitter<ContainerSelectedArgs>;
  @Event() graphUpdated: EventEmitter<GraphUpdatedArgs>;

  @Method()
  public async getGraph(): Promise<Graph> {
    return this.graph;
  }

  @Method()
  public async updateLayout(): Promise<void> {
    const width = this.el.clientWidth;
    const height = this.el.clientHeight;
    this.graph.resize(width, height);
    this.graph.updateBackground();
  }

  @Method()
  public async addActivity(args: AddActivityArgs): Promise<void> {
    const graph = this.graph;
    const {descriptor, x, y} = args;

    // TODO: Figure out how to convert client coordinates to appropriate graph coordinates taking into account transformations.
    // See https://x6.antv.vision/en/docs/api/graph/coordinate for documentation.
    //const point = graph.coord.localToClientPoint(x, y);
    const point: PointLike = {x, y};

    const sx = point.x;
    const sy = point.y;

    console.debug({
      client: {x, y},
      local: {sx, sy},
    });

    const activity: Activity = {
      id: uuid(),
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
  public async exportRoot(): Promise<Activity> {
    return this.exportRootInternal();
  }

  @Method()
  public async importRoot(root: Activity): Promise<void> {
    return this.importRootInternal(root);
  }

  public async componentDidLoad() {
    await this.createAndInitializeGraph();
  }

  public render() {

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

  public disableEvents = () => this.silent = true;

  public enableEvents = async (emitWorkflowChanged: boolean): Promise<void> => {
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

  private exportRootInternal = (): Activity => {
    const graph = this.graph;
    const graphModel = graph.toJSON();
    const activities = graphModel.cells.filter(x => x.shape == 'activity').map(x => x.data as Activity);
    const connections = graphModel.cells.filter(x => x.shape == 'elsa-edge' && !!x.data).map(x => x.data as Connection);
    const remainingConnections: Array<Connection> = []; // The connections remaining after transposition.
    let remainingActivities: Array<Activity> = [...activities]; // The activities remaining after transposition.
    const activityDescriptors = this.activityDescriptors;
    const transposeHandlerRegistry = Container.get(TransposeHandlerRegistry);

    // Transpose connections to activity outbound port properties.
    for (const connection of connections) {
      const source = activities.find(x => x.id == connection.source);
      const target = activities.find(x => x.id == connection.target);
      const sourceDescriptor = activityDescriptors.find(x => x.activityType == source.typeName);
      const targetDescriptor = activityDescriptors.find(x => x.activityType == target.typeName);
      const transposeHandler = transposeHandlerRegistry.get(source.typeName);

      if (transposeHandler.transpose({source, target, connection, sourceDescriptor, targetDescriptor})) {
        // Remove the target activity from the list.
        remainingActivities = remainingActivities.filter(x => x != target);
      } else {
        // Keep this connection.
        remainingConnections.push(connection);
      }
    }

    let rootActivities = activities.filter(activity => {
      const hasInboundConnections = connections.find(c => c.target == activity.id) != null;
      return !hasInboundConnections;
    });

    const rootActivity = rootActivities.find(x => x.canStartWorkflow) || first(rootActivities);

    return {
      typeName: 'Elsa.Flowchart',
      activities: remainingActivities,
      connections: remainingConnections,
      id: this.rootId,
      start: rootActivity?.id,
      metadata: {},
      applicationProperties: {},
      variables: []
    } as Flowchart;
  }

  private importRootInternal = async (root: Activity) => {
    this.rootId = root.id;
    const descriptors = this.activityDescriptors;
    const flowchart = root as Flowchart;
    const flowchartGraph = walkActivities(flowchart, this.activityDescriptors);
    const flowchartNodes = flattenList(flowchartGraph.children);
    const transposeHandlerRegistry = Container.get(TransposeHandlerRegistry);

    // Clear inbound port for start activity.
    const startActivityNode = flowchartNodes.find(x => x.activity.id === flowchart.start);

    if (startActivityNode?.port)
      delete startActivityNode.port;

    let edges: Array<Edge.Metadata> = [];

    // Create an X6 node for each activity.
    const nodes: Array<Node.Metadata> = flowchartNodes.map(activityNode => {
      const activity = activityNode.activity;
      const position = activity.metadata.designer?.position || {x: 100, y: 100};
      const {x, y} = position;
      const descriptor = descriptors.find(x => x.activityType == activity.typeName)
      return this.nodeFactory.createNode(descriptor, activity, x, y);
    });

    // Create X6 edges for each child activity.
    for (const rootNodes of flowchartNodes) {
      const childEdges = this.createEdges(rootNodes);
      edges = [...edges, ...childEdges];
    }

    // Create X6 edges for each connection in the flowchart.
    for (const connection of flowchart.connections) {
      const edge: Edge.Metadata = this.createEdge(connection);
      edges.push(edge);
    }

    const model: FromJSONData = {nodes, edges};

    // Freeze then unfreeze prevents an error from occurring when importing JSON a second time (e.g. after loading a new workflow.
    this.graph.freeze();
    this.graph.fromJSON(model, {silent: false});
    this.graph.unfreeze();
  };

  private createEdges = (activityNode: ActivityNode): Array<Edge.Metadata> => {
    let edges: Array<Edge.Metadata> = [];

    for (const childNode of activityNode.children) {
      const edge = this.createEdge({
        source: activityNode.activity.id,
        sourcePort: childNode.port,
        target: childNode.activity.id,
        targetPort: 'In'
      });

      edges.push(edge);
    }

    return edges;
  }

  // private createEdges = (connections: Array<Connection>): Array<Edge.Metadata> => {
  //   let edges: Array<Edge.Metadata> = [];
  //
  //   for (const connection of connections) {
  //     const edge = this.createEdge(connection);
  //     edges.push(edge);
  //   }
  //
  //   return edges;
  // }

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

  @Watch('root')
  async onRootChange(value: Activity) {
    await this.importRootInternal(value);
  }

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
    const node = e.node;
    const activity = node.data as Activity;
    const activityId = activity.id;

    const args: ActivitySelectedArgs = {
      activity: activity,
      applyChanges: a => {
        node.data = a;

        // If the ID of the activity changed, we need to update connection references (X6 stores deep copies of data).
        if (a.id !== activityId)
          this.syncEdgeData(activityId, a);
      },
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
    this.activityContextMenu.style.top = `${e.y}px`;
    this.activityContextMenu.style.left = `${e.x}px`;

    await this.activityContextMenu.open();
  }

  onNodeMoved = (e: PositionEventArgs<JQuery.ClickEvent>) => {
    const {node, x, y} = e;
    const activity = node.data as Activity;
    const nodePosition = node.position({relative: false});

    activity.metadata = {
      ...activity.metadata,
      designer: {
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
    this.graphUpdated.emit({exportGraph: this.exportRootInternal});
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

WorkflowEditorTunnel.injectProps(FlowchartComponent, ['activityDescriptors']);
