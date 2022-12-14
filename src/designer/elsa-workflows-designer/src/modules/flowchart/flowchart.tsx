import 'reflect-metadata';
import {Component, Element, Event, EventEmitter, h, Listen, Method, Prop, State, Watch} from '@stencil/core';
import {camelCase, first} from 'lodash';
import {Edge, Graph, Model, Node, NodeView, Point} from '@antv/x6';
import './shapes';
import './ports';
import {ActivityNodeShape} from './shapes';
import {Activity, ActivityDeletedArgs, ActivityDescriptor, ActivitySelectedArgs, ContainerSelectedArgs, EditChildActivityArgs, GraphUpdatedArgs, Port} from '../../models';
import {createGraph} from './graph-factory';
import {AddActivityArgs, Connection, Flowchart, FlowchartModel, FlowchartNavigationItem, RenameActivityArgs, UpdateActivityArgs} from './models';
import {NodeFactory} from "./node-factory";
import {Container} from "typedi";
import {ActivityNode, ContainerActivityComponent, createActivityLookup, EventBus, flatten, PortProviderRegistry, walkActivities} from "../../services";
import {ConnectionCreatedEventArgs, FlowchartEvents} from "./events";
import {ContextMenuAnchorPoint, MenuItemGroup} from "../../components/shared/context-menu/models";
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
import WorkflowDefinitionTunnel, {WorkflowDefinitionState} from "../../state/workflow-definition-state";

const FlowchartTypeName = 'Elsa.Flowchart';

@Component({
  tag: 'elsa-flowchart',
  styleUrl: 'flowchart.scss',
})
export class FlowchartComponent implements ContainerActivityComponent {
  private readonly eventBus: EventBus;
  private readonly nodeFactory: NodeFactory;
  private readonly portProviderRegistry: PortProviderRegistry;
  private activityContextMenu: HTMLElsaContextMenuElement;
  private currentFlowchart: Flowchart;
  private currentParent?: Activity;
  private currentPortName?: string;

  constructor() {
    this.eventBus = Container.get(EventBus);
    this.nodeFactory = Container.get(NodeFactory);
    this.portProviderRegistry = Container.get(PortProviderRegistry);
  }

  @Prop({mutable: true}) workflowDefinition: WorkflowDefinition; // Injected prop.
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

  @State() private activityLookup: Hash<Activity> = {};
  @State() private activities: Array<Activity> = [];
  @State() private activityNodes: Array<ActivityNode> = [];

  @Method()
  async newRoot(): Promise<Activity> {

    const flowchartDescriptor = this.getFlowchartDescriptor();
    const newName = await this.generateUniqueActivityName(flowchartDescriptor);
    const flowchart = await this.createFlowchart();
    await this.importInternal(flowchart);
    return flowchart;
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
    this.currentFlowchart = null;
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
  async autoLayout(direction: "TB" | "BT" | "LR" | "RL") {
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

    await this.import(this.currentFlowchart);
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

  @Method()
  async import(root: Activity): Promise<void> {
    await this.importInternal(root);
  }

  async componentDidLoad() {
    await this.createAndInitializeGraph();
  }

  @Listen('editChildActivity')
  private async editChildActivity(e: CustomEvent<EditChildActivityArgs>) {
    const currentActivity = this.currentFlowchart;
    const parentActivity = this.currentParent;
    const parentActivityDescriptor = descriptorsStore.activityDescriptors.find(x => x.typeName == parentActivity.type);
    const portName = e.detail.port.name;
    const portProvider = this.portProviderRegistry.get(parentActivity.type);
    let childActivity = portProvider.resolvePort(portName, {activity: parentActivity, activityDescriptor: parentActivityDescriptor}) as Flowchart;

    if (!childActivity) {
      childActivity = await this.createFlowchart();
      portProvider.assignPort(portName, childActivity, {activity: parentActivity, activityDescriptor: parentActivityDescriptor});
    }

    this.currentParent = currentActivity;
    this.currentPortName = portName;

    await this.setupGraph(childActivity);
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
      applicationProperties: {},
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

  private updateModel = async () => {
    const model = this.getFlowchartModel();
    // let currentLevel = this.getCurrentContainerInternal();
    //
    // if (!currentLevel) {
    //   currentLevel = await this.createContainer();
    // }
    //
    // currentLevel.activities = model.activities;
    // currentLevel.connections = model.connections;
    // currentLevel.start = model.start;
    //
    // const currentPath = this.currentPath;
    // const currentNavigationItem = currentPath[currentPath.length - 1];
    // const currentPortName = currentNavigationItem?.portName;
    // const currentScope = this.activityLookup[currentNavigationItem.activityId] as Activity;
    //
    // const currentScopeDescriptor = this.getActivityDescriptor(currentScope.type);
    //
    // if (!!currentPortName) {
    //   if (currentLevel != currentScope) {
    //     const portProvider = this.portProviderRegistry.get(currentScope.type);
    //     portProvider.assignPort(currentPortName, currentLevel, {activity: currentScope, activityDescriptor: currentScopeDescriptor});
    //   }
    // } else {
    //   this.activity = currentLevel;
    // }

    this.updateLookups();
  }

  private exportInternal = async (): Promise<Activity> => {

    // Get flowchart model.
    const model = this.getFlowchartModel();
    const activity: Flowchart = {...this.currentFlowchart};
    activity.connections = model.connections;
    activity.activities = model.activities;
    activity.start = model.start;

    // If we have a current parent, update its port.
    if (!!this.currentParent && !!this.currentPortName) {
      const portProvider = this.portProviderRegistry.get(this.currentParent.type);
      const parentActivityDescriptor = descriptorsStore.activityDescriptors.find(x => x.typeName == this.currentParent.type);
      portProvider.assignPort(this.currentPortName, activity, {activity: this.currentParent, activityDescriptor: parentActivityDescriptor});
    }

    return activity;
  }

  private importInternal = async (root: Activity) => {
    const flowchart = root as Flowchart;
    this.currentFlowchart = flowchart;

    // if (!this.currentPath || this.currentPath.length == 0)
    //   this.currentPath = [{activityId: flowchart.id, portName: null, index: 0}];

    await this.setupGraph(flowchart);
  };

  private getActivityDescriptor = (typeName: string): ActivityDescriptor => descriptorsStore.activityDescriptors.find(x => x.typeName == typeName)

  private setupGraph = async (flowchart: Flowchart) => {
    const activities = flowchart.activities;
    const connections = flowchart.connections;
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
    await this.scrollToStart();
  };

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

  // private getCurrentContainerInternal(): Flowchart {
  //   const currentItem = this.currentPath.length > 0 ? this.currentPath[this.currentPath.length - 1] : null;
  //
  //   if (!currentItem)
  //     return this.activity;
  //
  //   const activity = this.activityLookup[currentItem.activityId] as Flowchart;
  //   const activityDescriptor = descriptorsStore.activityDescriptors.find(x => x.typeName == activity.type);
  //
  //   if (activityDescriptor.isContainer)
  //     return activity;
  //
  //   const portProvider = this.portProviderRegistry.get(activity.type);
  //   return portProvider.resolvePort(currentItem.portName, {activity, activityDescriptor}) as Flowchart;
  // }

  private updateLookups = () => {
    this.activityNodes = flatten(walkActivities(this.workflowDefinition.root));
    this.activities = this.activityNodes.map(x => x.activity);
    this.activityLookup = createActivityLookup(this.activityNodes);
  }

  private generateUniqueActivityName = async (activityDescriptor: ActivityDescriptor): Promise<string> => {
    return await generateUniqueActivityName(this.activities, activityDescriptor);
  };

  private getAllActivities = (): Array<Activity> => this.activities;

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
    //const currentContainer = this.getCurrentContainerInternal();

    const args: ContainerSelectedArgs = {
      activity: this.currentFlowchart,
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

  // private onNavigateHierarchy = async (e: CustomEvent<FlowchartNavigationItem>) => {
  //   const item = e.detail;
  //   const activityId = item.activityId;
  //   let activity = this.activityLookup[activityId];
  //   const activityDescriptor = descriptorsStore.activityDescriptors.find(x => x.typeName == activity.type);
  //   const path = this.currentPath;
  //   const index = path.indexOf(item);
  //
  //   this.currentPath = path.slice(0, index + 1);
  //
  //   if (!!item.portName) {
  //     if (!activityDescriptor.isContainer) {
  //       const portName = camelCase(item.portName);
  //       activity = activity[portName] as Activity;
  //     }
  //   }
  //
  //   await this.importInternal(activity);
  // }

  render() {
    const activity = this.currentFlowchart;

    const state: FlowchartState = {
      flowchart: activity,
      nodeMap: this.activityLookup
    }

    return (
      <FlowchartTunnel.Provider state={state}>
        <div class="relative">
          {/*<div class="absolute left-0 top-3 z-10">*/}
          {/*  <elsa-workflow-navigator items={this.currentPath} workflowDefinition={this.workflowDefinition} onNavigate={this.onNavigateHierarchy}/>*/}
          {/*</div>*/}
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

WorkflowDefinitionTunnel.injectProps(FlowchartComponent, ['workflowDefinition']);
