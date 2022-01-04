import {Component, Element, Event, EventEmitter, h, Method, Prop, Watch} from '@stencil/core';
import {Edge, Graph, Model, Node, NodeView} from '@antv/x6';
import {v4 as uuid} from 'uuid';
import {camelCase, first} from 'lodash';
import './shapes';
import './ports';
import {ContainerActivityComponent} from '../container-activity-component';
import {AddActivityArgs} from '../../designer/canvas/canvas';
import {Activity, ActivityDescriptor, ActivitySelectedArgs, ContainerSelectedArgs, GraphUpdatedArgs} from '../../../models';
import {createGraph} from './graph-factory';
import {createNode} from './node-factory';
import {Connection, Flowchart} from './models';
import WorkflowEditorTunnel from '../../designer/state';
import {ActivityNode, flattenList, walkActivities} from "./activity-walker";
import PositionEventArgs = NodeView.PositionEventArgs;
import FromJSONData = Model.FromJSONData;

@Component({
  tag: 'elsa-flowchart',
  styleUrl: 'flowchart.scss',
})
export class FlowchartComponent implements ContainerActivityComponent {
  private rootId: string = uuid();

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

    const activity: Activity = {
      id: uuid(),
      nodeType: descriptor.nodeType,
      metadata: {
        designer: {
          position: {
            x,
            y
          }
        }
      },
    };

    const node = createNode(descriptor, activity, x, y);
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
      <div
        class="absolute left-0 top-0 right-0 bottom-0"
        ref={el => this.container = el}/>
    );
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
    });

    graph.on('blank:click', this.onGraphClick);
    graph.on('node:click', this.onNodeClick);
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

    // Transpose connections to activity outbound port properties.
    for (const connection of connections) {
      const source = activities.find(x => x.id == connection.source);
      const target = activities.find(x => x.id == connection.target);
      const sourceDescriptor = activityDescriptors.find(x => x.nodeType == source.nodeType);
      const matchingTargetPort = sourceDescriptor.outPorts.find(x => x.name == connection.sourcePort);

      if (!!matchingTargetPort) {
        // Assign the target activity directly to the outbound port of the source activity.
        const outPortPropName = camelCase(connection.sourcePort);
        source[outPortPropName] = target;

        // Remove the target activity from the list.
        remainingActivities = remainingActivities.filter(x => x != target);
      } else {
        // Keep this connection.
        remainingConnections.push(connection);
      }
    }

    return {
      nodeType: 'Workflows.Flowchart',
      activities: remainingActivities,
      connections: remainingConnections,
      id: this.rootId,
      start: first(activities)?.id,
      metadata: {},
      variables: []
    } as Flowchart;
  }

  private importRootInternal = async (root: Activity) => {
    this.rootId = root.id;
    const descriptors = this.activityDescriptors;
    const flowchart = root as Flowchart;
    const flowchartGraph = walkActivities(root, this.activityDescriptors);
    const flowchartNodes = flattenList(flowchartGraph.children);
    const nodes: Array<Node.Metadata> = [];
    let edges: Array<Edge.Metadata> = [];

    // Create an X6 node for each activity.
    for (const activityNode of flowchartNodes) {
      const activity = activityNode.activity;
      const position = activity.metadata.designer?.position || {x: 100, y: 100};
      const {x, y} = position;
      const descriptor = descriptors.find(x => x.nodeType == activity.nodeType)
      const node = createNode(descriptor, activity, x, y);

      nodes.push(node);

      // Create X6 edges for each child activity.
      const childEdges = this.createEdges(activityNode);
      edges = [...childEdges];
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
        sourcePort: activityNode.port,
        target: childNode.activity.id,
        targetPort: childNode.port
      });

      edges.push(edge);
    }

    return edges;
  }

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

  @Watch('root')
  private async onRootChange(value: Activity) {
    await this.importRootInternal(value);
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

  private onGraphClick = async (e: PositionEventArgs<JQuery.ClickEvent>) => this.containerSelected.emit({});

  private onNodeClick = async (e: PositionEventArgs<JQuery.ClickEvent>) => {
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

  private onNodeMoved = (e: PositionEventArgs<JQuery.ClickEvent>) => {
    const {node, x, y} = e;
    const activity = node.data as Activity;

    activity.metadata = {
      ...activity.metadata,
      designer: {
        position: {
          x,
          y
        }
      }
    }
  }

  private onEdgeConnected = (e: { isNew: boolean, edge: Edge }) => {
    const edge = e.edge;
    const sourceNode = edge.getSourceNode();
    const targetNode = edge.getTargetNode();
    const sourceActivity = edge.getSourceNode().data as Activity;
    const targetActivity = targetNode.data as Activity;
    const sourcePort = sourceNode.getPort(edge.getSourcePortId()).id;
    const targetPort = targetNode.getPort(edge.getTargetPortId()).id;

    edge.data = {
      source: sourceActivity.id,
      sourcePort: sourcePort,
      target: targetActivity.id,
      targetPort: targetPort
    } as Connection;
  }

  private onGraphChanged = async (e: any) => {
    this.graphUpdated.emit({exportGraph: this.exportRootInternal});
  }
}

WorkflowEditorTunnel.injectProps(FlowchartComponent, ['activityDescriptors']);
