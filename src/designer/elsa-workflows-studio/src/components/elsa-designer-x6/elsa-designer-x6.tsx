import {Component, Event, EventEmitter, h, Host, Method, Prop, State, Watch} from '@stencil/core';
import {v4 as uuid} from 'uuid';
import {
  addConnection,
  findActivity,
  getChildActivities,
  getInboundConnections,
  getOutboundConnections,
  Map,
  removeActivity,
  removeConnection,
  Hash
} from '../../utils/utils';
import {
  ActivityDescriptor,
  ActivityDesignDisplayContext,
  ActivityModel,
  ActivityTraits,
  ConnectionModel,
  EventTypes,
  WorkflowModel,
  WorkflowPersistenceBehavior,
  ActivityDefinitions,
  Flowchart,
  FlowchartNavigationItem,
  ActivityDeletedArgs,
} from '../../models';
import {eventBus,
  PortProviderRegistry,
  ActivityNode,
} from '../../services';
import state from '../../utils/store';
import {ActivityIcon} from '../icons/activity-icon';
import {ActivityContextMenuState, LayoutDirection, WorkflowDesignerMode} from "../designers/tree/elsa-designer-tree/models";
import {createGraph} from './graph-factory';
import {Edge, Graph, Model, Node, Cell} from '@antv/x6';
import  { ActivityNodeShape } from "./shapes";
import {Container} from "typedi";

const FlowchartTypeName = 'Workflow';

@Component({
  tag: 'elsa-designer-x6',
  styleUrls: ['elsa-designer-x6.css'],
  assetsDirs: ['assets'],
  shadow: false,
})
export class ElsaWorkflowDesigner {
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

  el: HTMLElement;
  parentActivityId?: string;
  parentActivityOutcome?: string;
  addingActivity: boolean = false;
  activityDisplayContexts: Map<ActivityDesignDisplayContext> = null;
  oldActivityDisplayContexts: Map<ActivityDesignDisplayContext> = null;
  selectedActivities: Map<ActivityModel> = {};
  ignoreCopyPasteActivities: boolean = false;

  constructor() {
    this.portProviderRegistry = Container.get(PortProviderRegistry);
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

  @Watch('model')
  handleModelChanged(newValue: WorkflowModel) {
    this.updateWorkflowModel(newValue, false);
  }

  @Watch('selectedActivityIds')
  handleSelectedActivityIdsChanged(newValue: Array<string>) {
    const ids = newValue || [];
    const selectedActivities = this.workflowModel.activities.filter(x => ids.includes(x.activityId));
    const map: Map<ActivityModel> = {};

    for (const activity of selectedActivities)
      map[activity.activityId] = activity;

    this.selectedActivities = map;
  }

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

  // @Listen('keydown', {target: 'window'})
  // async handleKeyDown(event: KeyboardEvent) {
  //   if (this.ignoreCopyPasteActivities)
  //     return;
  //
  //   if ((event.ctrlKey || event.metaKey) && event.key === 'c') {
  //     await this.copyActivitiesToClipboard();
  //   }
  //   if ((event.ctrlKey || event.metaKey) && event.key === 'v') {
  //
  //     await this.pasteActivitiesFromClipboard();
  //   }
  // }

  @Method()
  async removeActivity(activity: ActivityModel) {
    this.removeActivityInternal(activity);
  }

  @Method()
  async removeSelectedActivities() {
    let model = {...this.workflowModel};

    Object.keys(this.selectedActivities).forEach((key) => {
      model = this.removeActivityInternal(this.selectedActivities[key], model);
    });

    this.updateWorkflowModel(model);
  }

  @Method()
  async showActivityEditor(activity: ActivityModel, animate: boolean) {
    await this.showActivityEditorInternal(activity, animate);
  }

  // async copyActivitiesToClipboard() {
  //   await navigator.clipboard.writeText(JSON.stringify(this.selectedActivities));
  //   await eventBus.emit(EventTypes.ClipboardCopied, this);
  // }

  // async pasteActivitiesFromClipboard() {
  //   let copiedActivities: Array<ActivityModel> = [];
  //
  //   await navigator.clipboard.readText().then(data => {
  //     copiedActivities = JSON.parse(data);
  //   });
  //   await this.addActivitiesFromClipboard(copiedActivities)
  // }

  getOutcomes = (activity: ActivityModel, definition: ActivityDefinitions): Array<string> => {
    let outcomes = [];
    const displayContext = this.activityDisplayContexts[activity.activityId];

    if (!!definition) {
      const lambda = displayContext?.outcomes || definition.outcomes;

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

  private getActivitiesList(): Array<ActivityModel> {
    let newList = [];
    Object.entries(this.activityDisplayContexts).forEach(([key, value]) => newList.push(value));
    return newList;
  }

  private init = () => {
    this.updateGraph();
    this.graph.centerContent();
  }

  private createGraphActivity = (item: ActivityModel) => {
    const displayContext = this.activityDisplayContexts[item.activityId];
    const activityDefinitions: Array<ActivityDescriptor> = state.activityDescriptors;
    const definition = activityDefinitions.find(x => x.type == item.type);
    const outcomes: Array<string> = this.mode === WorkflowDesignerMode.Blueprint ? item.outcomes : this.getOutcomes(item, definition);
    let ports = [{group: 'in', id: uuid(), attrs: {}},]
      outcomes.forEach(outcome => ports.push({
        id: outcome,
        group: 'out',
        attrs: {
          text: {
            text: outcome
          },
        }
      }));

    return { id: item.activityId,
      shape: 'activity',
      x: item.x || 0,
      y: item.y || 0,
      type: item.type,
      activity: item,
      ports: {
        items: ports
      },
      component: this.renderActivity(item),
      selectedActivities: this.selectedActivities,
      activityDescriptor: displayContext.activityDescriptor,
      displayContext: displayContext,
      activityDisplayContexts: this.activityDisplayContexts
   };
  }

  private readonly portProviderRegistry: PortProviderRegistry;
  private silent: boolean = false; // Whether to emit events or not.
  private activity: Flowchart;

  private disableEvents = () => this.silent = true;

  container: HTMLElement;
  graph: Graph;

  @Event() activityDeleted: EventEmitter<ActivityDeletedArgs>;

  @State() private currentPath: Array<FlowchartNavigationItem> = [];
  @State() private activityLookup: Hash<ActivityModel> = {};
  @State() private activityNodes: Array<ActivityNode> = [];

  // @Event() graphUpdated: EventEmitter<GraphUpdatedArgs>;
  // @Event() containerSelected: EventEmitter<ContainerSelectedArgs>;

  @Method()
  async updateLayout(): Promise<void> {
    const width = this.el.clientWidth;
    const height = this.el.clientHeight;
    this.graph.resize(width, height);
    this.graph.updateBackground();
  }


  async componentDidLoad() {
    await this.createAndInitializeGraph();
    this.init();
  }

  private enableEvents = async (emitWorkflowChanged: boolean): Promise<void> => {
    this.silent = false;

    if (emitWorkflowChanged === true) {
      // await this.updateGraph();
    }
  };

  private createAndInitializeGraph = async () => {
    const graph = this.graph = createGraph(this.container,
      {},
      this.pasteActivities,
      this.disableEvents,
      this.enableEvents);

    graph.on('blank:click', this.onGraphClick);
    graph.on('node:click', this.onNodeClick);
    graph.on('node:contextmenu', this.onNodeContextMenu);
    graph.on('edge:connected', this.onEdgeConnected);
    graph.on('edge:removed', this.connectionDetachedTest);
    graph.on('node:moved', this.onNodeMoved);
    // graph.on('node:change:*', this.onGraphChanged);
    // graph.on('node:added', this.onGraphChanged);
    graph.on('node:removed', this.onNodeRemoved);
    // graph.on('edge:added', this.onGraphChanged);
    // graph.on('edge:removed', this.onGraphChanged);
    // graph.on('edge:connected', this.onGraphChanged);

    await this.updateLayout();
  }

  private pasteActivities = async (activities?: Array<any>, connections?: Array<ConnectionModel>) => {
    const activityDescriptors: Array<ActivityDescriptor> = state.activityDescriptors;

    let workflowActivities = this.workflowModel.activities;
    let workflowConnections = this.workflowModel.connections;

    for (const activity of activities) {
      const activityDescriptor = activityDescriptors.find(descriptor => descriptor.type === activity.type);

      let newActivity: ActivityModel = {
        activityId: activity.activityId,
        type: activityDescriptor.type,
        outcomes: activityDescriptor.outcomes || activity.outcomes,
        displayName: activityDescriptor.displayName,
        properties: activity.properties,
        propertyStorageProviders: {},
        x: activity.x + 32,
        y: activity.y + 32,
      };
      // x and y - according to graph.paste({offset: 32})
      for (const property of activityDescriptor.inputProperties) {
        newActivity.properties[property.name] = {
          syntax: '',
          expression: '',
        };
      };
      workflowActivities.push(newActivity);
      const activityDisplayContext = await this.getActivityDisplayContext(newActivity);
      this.activityDisplayContexts[newActivity.activityId] = activityDisplayContext;
    }

    for (const connection of connections ) {
      workflowConnections.push(connection)
    }

    let workflowModel = {...this.workflowModel, activities: workflowActivities, connections: workflowConnections};
    this.updateWorkflowModel(workflowModel);
    this.updateGraph();
  }

  private removeConnectionTest(workflowModel: WorkflowModel, sourceId: string, outcome: string): WorkflowModel {
    return {
      ...workflowModel,
      connections: workflowModel.connections.filter(x => !(x.sourceId === sourceId && x.outcome === outcome))
    };
  }

  private onNodeMoved = async (e) => {
    const node = e.node as ActivityNodeShape;
    const activity = node.activity as ActivityModel;
    const nodePosition = node.position({relative: false});

    activity.x = nodePosition.x;
    activity.y = nodePosition.y;

    this.updateActivity(activity);
  }

  private onNodeClick = async (e) => {
    const node = e.node as ActivityNodeShape;
    const activity = node.activity as ActivityModel;

    // const args: ActivitySelectedArgs = {
    //   activity: activity,
    // };
    // this.activitySelected.emit(args);

     // If a parent activity was selected to connect to:
     if (this.mode == WorkflowDesignerMode.Edit && this.parentActivityId && this.parentActivityOutcome) {
      this.addConnection(this.parentActivityId, node.id, this.parentActivityOutcome);
    } else {
      if (!!this.selectedActivities[activity.activityId])
        delete this.selectedActivities[activity.activityId];
      else {
        for (const key in this.selectedActivities) {
          this.activityDeselected.emit(this.selectedActivities[key]);
        }
        this.selectedActivities = {};
        this.selectedActivities[activity.activityId] = activity;
        this.activitySelected.emit(activity);
      }
    }

    if (this.mode == WorkflowDesignerMode.Edit || this.mode == WorkflowDesignerMode.Instance) {

      //  '.context-menu-button-container button'
      e.e.stopPropagation();
      this.handleContextMenuChange({
        x: e.e.clientX,
        y: e.e.clientY,
        shown: true,
        activity: node.activity,
        selectedActivities: this.selectedActivities
      });
    }

    if (this.mode == WorkflowDesignerMode.Test) {

        //'.context-menu-button-container button'
        //   e.stopPropagation();
        //   this.handleContextMenuTestChange({x: e.clientX, y: e.clientY, shown: true, activity: node.activity});
    }
  };

  private onNodeContextMenu = (e) => {
    e.preventDefault();
    e.stopPropagation();
    this.parentActivityId = e.node.activity.activityId;
    this.parentActivityOutcome = e.node.outcome;
    this.handleConnectionContextMenuChange({x: e.clientX, y: e.clientY, shown: true, activity: e.node.activity});
  }

  // private onGraphClick = async (e: PositionEventArgs<JQuery.ClickEvent>) => {
  //   const currentContainer = this.getCurrentContainerInternal();

  //   const args: ContainerSelectedArgs = {
  //     activity: currentContainer,
  //   };
  //   return this.containerSelected.emit(args);
  // };

  private onGraphClick = async () => {
    // const currentContainer = this.getCurrentContainerInternal();

    // const args: ContainerSelectedArgs = {
    //   activity: currentContainer,
    // };
    // return this.containerSelected.emit(args);
    for (const key in this.selectedActivities) {
      this.activityDeselected.emit(this.selectedActivities[key]);
    }
    this.selectedActivities = {};
  };

  // private onGraphChanged = async (e) => {
    // if (this.silent) {
    //   await this.enableEvents(false);
    //   return;
    // }

    // this.graphUpdated.emit();
  //   await this.updateLayout();
  // }

  addConnectionTest(sourceActivityId: string, targetActivityId: string, outcome: string) {
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
  }

  private connectionDetachedTest = async (e: { edge: Edge }) => {
    const edge = e.edge;
    let workflowModel = {...this.workflowModel};
    const sourceId = edge.getSourceCellId();
    const outcome: string = edge.getSourcePortId();
    workflowModel = this.removeConnectionTest(workflowModel, sourceId, outcome);
    this.updateWorkflowModel(workflowModel);
  };

  private onEdgeConnected = async (e: { edge: Edge }) => {
    const edge = e.edge;
    let workflowModel = {...this.workflowModel};

    const sourceNode = edge.getSourceNode();
    const targetId = edge.getTargetCellId();
    const sourcePort = edge.getSourcePortId();

    this.addConnectionTest(sourceNode.id, targetId, sourcePort);

    if (!this.enableMultipleConnectionsFromSingleSource) {
      const existingConnection = workflowModel.connections.find(x => x.sourceId === sourceNode.id && x.targetId == targetId);

      if (!existingConnection) {
        // Add created connection to list.
        const newConnection: ConnectionModel = {
          sourceId: sourceNode.id,
          targetId: targetId,
          outcome: sourcePort
        };

        edge.insertLabel(sourcePort)

        workflowModel.connections = [...workflowModel.connections, newConnection];
        this.updateWorkflowModel(workflowModel);
      }
  }}

  private createEdge = (connection: ConnectionModel): Edge.Metadata => {
    return {
      shape: 'elsa-edge',
      zIndex: -1,
      data: connection,
      source: {cell: connection.sourceId, port: connection.outcome},
      target: connection.targetId,
      outcome: connection.outcome,
      labels:
        [{
          attrs: {
            label: { text: connection.outcome }
          },
        }],
    };
  }

  private updateGraph = () => {
    const activities = this.workflowModel.activities;
    const connections = this.workflowModel.connections;
    const edges: Array<Edge.Metadata> = [];

    // Create an X6 node for each activity.
    const nodes: Array<Node.Metadata> = activities.map(activity => this.createGraphActivity(activity));

    // Create X6 edges for each connection in the flowchart.
    for (const connection of connections) {
      const edge: Edge.Metadata = this.createEdge(connection);
      edges.push(edge);
    }

    const model: Model.FromJSONData = {nodes, edges};

    this.disableEvents();
    // Freeze then unfreeze prevents an error from occurring when importing JSON a second time (e.g. after loading a new workflow.
    this.graph.freeze();
    this.graph.fromJSON(model, {silent: false});
    this.graph.unfreeze();
    this.enableEvents(false);
  }

  // private getActivityDescriptor = (typeName: string): ActivityDescriptor => {
  //   return state.activityDescriptors.find(x => x.type == typeName)}

  private onNodeRemoved = (e: any) => {
    const activity = e.node.activity as ActivityModel;
    this.removeActivity(activity);
    this.activityDeselected.emit(activity);
    this.activityDeleted.emit({activity});
  };

  async onAddActivity(e: Event) {
    e.preventDefault();
    if (this.mode !== WorkflowDesignerMode.Test) await this.showActivityPicker();
  }

  private findActivityByElement = (element: Element): ActivityModel => {
    const id =  ElsaWorkflowDesigner.getActivityId(element);
    return this.findActivityById(id);
  };

  private static getActivityId(element: Element): string {
    return element.attributes['data-cell-id'].value;
  }

  private findActivityById = (id: string): ActivityModel => this.workflowModel.activities.find(x => x.activityId === id);

  async addActivitiesFromClipboard(copiedActivities: Array<ActivityModel>) {
    let sourceActivityId: string;
    this.parentActivityId = null;
    this.parentActivityOutcome = null;

    for (const key in this.selectedActivities) {
      sourceActivityId = this.selectedActivities[key].activityId;
    }

    if (sourceActivityId != undefined) {
      this.parentActivityId = sourceActivityId;
      this.parentActivityOutcome = this.selectedActivities[sourceActivityId].outcomes[0];
    }

    for (const key in copiedActivities) {
      this.addingActivity = true;
      copiedActivities[key].activityId = uuid();

      await eventBus.emit(EventTypes.UpdateActivity, this, copiedActivities[key]);

      this.parentActivityId = copiedActivities[key].activityId;
      this.parentActivityOutcome = copiedActivities[key].outcomes[0];
    }

    this.selectedActivities = {};
    // Set to null to avoid conflict with on Activity node click event
    this.parentActivityId = null;
    this.parentActivityOutcome = null;
  }

  connectedCallback() {
    eventBus.on(EventTypes.ActivityPicked, this.onActivityPicked);
    eventBus.on(EventTypes.UpdateActivity, this.onUpdateActivity);
    //eventBus.on(EventTypes.PasteActivity, this.onPasteActivity);
    // eventBus.on(EventTypes.HideModalDialog, this.onCopyPasteActivityEnabled);
    // eventBus.on(EventTypes.ShowWorkflowSettings, this.onCopyPasteActivityDisabled);
    eventBus.on(EventTypes.WorkflowExecuted, this.onWorkflowExecuted);
  }

  disconnectedCallback() {
    eventBus.detach(EventTypes.ActivityPicked, this.onActivityPicked);
    eventBus.detach(EventTypes.UpdateActivity, this.onUpdateActivity);
    //eventBus.detach(EventTypes.PasteActivity, this.onPasteActivity);
    // eventBus.detach(EventTypes.HideModalDialog, this.onCopyPasteActivityEnabled);
    // eventBus.detach(EventTypes.ShowWorkflowSettings, this.onCopyPasteActivityDisabled);
    eventBus.detach(EventTypes.WorkflowExecuted, this.onWorkflowExecuted);
  }

  componentWillLoad() {
    this.workflowModel = this.model;
  }

  // componentDidLoad() {
  //   this.svgD3Selected = d3.select(this.svg);
  //   this.innerD3Selected = d3.select(this.inner);
  //   this.safeRender();
  // }

  // safeRender() {
  //   // Rebuild D3 model if component completed its initial load.
  //   if (!this.svgD3Selected)
  //     return;

  //   const rect = this.el.getBoundingClientRect();
  //   if (rect.height === 0 || rect.width === 0) {
  //     const observer = new ResizeObserver(entries => {
  //       for (let entry of entries) {
  //         // this.renderTree();
  //         observer.unobserve(this.el);
  //       }
  //     });

  //     observer.observe(this.el);
  //   } else {
  //     // this.renderTree();
  //   }
  // }

  async componentWillRender() {
    if (!!this.activityDisplayContexts)
      return;

    const activityModels = this.workflowModel.activities;
    const displayContexts: Map<ActivityDesignDisplayContext> = {};

    for (const model of activityModels)
      displayContexts[model.activityId] = await this.getActivityDisplayContext(model);

    this.activityDisplayContexts = displayContexts;
    // this.safeRender();
  }

  async getActivityDisplayContext(activityModel: ActivityModel): Promise<ActivityDesignDisplayContext> {
    const activityDescriptors: Array<ActivityDescriptor> = state.activityDescriptors;
    let descriptor = activityDescriptors.find(x => x.type == activityModel.type);
    let descriptorExists = !!descriptor;
    const oldContextData = (this.oldActivityDisplayContexts && this.oldActivityDisplayContexts[activityModel.activityId]) || {expanded: false};

    if (!descriptorExists)
      descriptor = this.createNotFoundActivityDescriptor(activityModel);

    const description = descriptorExists ? activityModel.description : `(Not Found) ${descriptorExists}`;
    const bodyText = description && description.length > 0 ? description : undefined;
    const bodyDisplay = bodyText ? `<p>${bodyText}</p>` : undefined;
    const color = (descriptor.traits &= ActivityTraits.Trigger) == ActivityTraits.Trigger ? 'rose' : 'sky';
    const displayName = descriptorExists ? activityModel.displayName : `(Not Found) ${activityModel.displayName}`;

    const displayContext: ActivityDesignDisplayContext = {
      activityModel: activityModel,
      activityDescriptor: descriptor,
      activityIcon: <ActivityIcon color={color}/>,
      bodyDisplay: bodyDisplay,
      displayName: displayName,
      outcomes: [...activityModel.outcomes],
      expanded: oldContextData.expanded
    };

    await eventBus.emit(EventTypes.ActivityDesignDisplaying, this, displayContext);
    return displayContext;
  }

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
    this.workflowModel = this.cleanWorkflowModel(model);
    this.oldActivityDisplayContexts = this.activityDisplayContexts;
    // this.activityDisplayContexts = null;

    if (emitEvent)
      this.workflowChanged.emit(model);
  }

  cleanWorkflowModel(model: WorkflowModel): WorkflowModel {

    // Detect duplicate activities and throw.
    const activityIds = model.activities.map(x => x.activityId);
    const count = ids => ids.reduce((a, b) => ({...a, [b]: (a[b] || 0) + 1}), {})
    const duplicates = dict => Object.keys(dict).filter((a) => dict[a] > 1)
    const duplicateIds = duplicates(count(activityIds));

    if (duplicateIds.length > 0) {
      console.error(duplicateIds);
      throw Error(`Found duplicate activities. Throwing for now until we find the root cause.`);
    }

    model.connections = model.connections.filter(connection => {
      const sourceId = connection.sourceId;
      const targetId = connection.targetId;
      const sourceExists = model.activities.findIndex(x => x.activityId == sourceId) != null;
      const targetExists = model.activities.findIndex(x => x.activityId == targetId) != null;

      if (!sourceExists)
        connection.sourceId = null;

      if (!targetExists)
        connection.targetId = null;

      return !!connection.sourceId || !!connection.targetId;
    });

    return model;
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
    const activity: ActivityModel = {
      activityId: uuid(),
      type: activityDescriptor.type,
      outcomes: activityDescriptor.outcomes,
      displayName: activityDescriptor.displayName,
      properties: [],
      propertyStorageProviders: {},
      x: Math.round(this.graph.getContentBBox().x),
      y: Math.round(this.graph.getContentBBox().y)
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
    const activityDisplayContext = await this.getActivityDisplayContext(activity);

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
          outcome: activityDisplayContext.outcomes[0]
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

    this.updateWorkflowModel(workflowModel);
    this.activityDisplayContexts[activity.activityId] = activityDisplayContext;
    this.updateGraph();
    this.parentActivityId = null;
    this.parentActivityOutcome = null;
  }

  getRootActivities(): Array<ActivityModel> {
    return getChildActivities(this.workflowModel, null);
  }

  addConnection(sourceActivityId: string, targetActivityId: string, outcome: string) {
    const workflowModel = {...this.workflowModel};
    const newConnection: ConnectionModel = {sourceId: sourceActivityId, targetId: targetActivityId, outcome: outcome};
    let connections = workflowModel.connections;

    if (!this.enableMultipleConnectionsFromSingleSource)
      connections = [...workflowModel.connections.filter(x => !(x.sourceId === sourceActivityId && x.outcome === outcome))];

    workflowModel.connections = [...connections, newConnection];
    this.updateWorkflowModel(workflowModel);
    this.parentActivityId = null;
    this.parentActivityOutcome = null;
  }

  async updateActivity(activity: ActivityModel) {
    let workflowModel = {...this.workflowModel};
    const activities = [...workflowModel.activities];
    const index = activities.findIndex(x => x.activityId === activity.activityId);
    activities[index] = activity;
    this.updateWorkflowModel({...workflowModel, activities: activities});

    const activityDisplayContext = await this.getActivityDisplayContext(activity);
    this.activityDisplayContexts[activity.activityId] = activityDisplayContext;
    this.updateGraph();
  }

  async showActivityPicker() {
    await eventBus.emit(EventTypes.ShowActivityPicker);
  }

  removeConnection(sourceId: string, outcome: string) {
    let workflowModel = {...this.workflowModel};
    workflowModel = removeConnection(workflowModel, sourceId, outcome);
    this.updateWorkflowModel(workflowModel);
  }

  onActivityPicked = async args => {
    const activityDescriptor = args as ActivityDescriptor;
    const activityModel = this.newActivity(activityDescriptor);

    const connectFromRoot = !this.parentActivityOutcome || this.parentActivityOutcome == '';
    const sourceId = connectFromRoot ? null : this.parentActivityId;
    const targetId = connectFromRoot ? this.parentActivityId : null;
    this.addActivity(activityModel, sourceId, targetId, this.parentActivityOutcome);
  };

  onUpdateActivity = args => {
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
      this.updateActivity(activityModel);
    }
  };

  // onPasteActivity = async args => {
  //   const activityModel = args as ActivityModel;
  //
  //   this.selectedActivities = {};
  //   activityModel.outcomes[0] = this.parentActivityOutcome;
  //   this.selectedActivities[activityModel.activityId] = activityModel;
  //   await this.pasteActivitiesFromClipboard();
  // };
  //
  // onCopyPasteActivityEnabled = () => {
  //   this.ignoreCopyPasteActivities = false
  // }
  //
  // onCopyPasteActivityDisabled = () => {
  //   this.ignoreCopyPasteActivities = true
  // }

  onWorkflowExecuted = () => {
  }

  createActivityOptions(activity: ActivityModel) {
    return {
      shape: 'rect',
      label: this.renderActivity(activity),
      rx: 5,
      ry: 5,
      labelType: 'html',
      class: 'activity',
      activity,
    };
  }

  createOutcomeActivityOptions() {
    return {shape: 'circle', label: this.renderOutcomeButton(), labelType: 'html', class: 'add', width: 32, height: 32};
  }

  renderOutcomeButton() {
    const cssClass = this.mode == WorkflowDesignerMode.Edit ? 'hover:elsa-text-blue-500 elsa-cursor-pointer' : 'elsa-cursor-default';
    return `<svg class="elsa-h-8 elsa-w-8 elsa-text-gray-400 ${cssClass}" fill="none" viewBox="0 0 24 24" stroke="currentColor">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v3m0 0v3m0-3h3m-3 0H9m12 0a9 9 0 11-18 0 9 9 0 0118 0z" />
      </svg>`;
  }

  renderActivity(activity: ActivityModel) {
    const activityDisplayContexts = this.activityDisplayContexts || {};
    const displayContext = activityDisplayContexts[activity.activityId] || undefined;
    const activityBorderColor = !!this.activityBorderColor ? this.activityBorderColor(activity) : 'gray';
    const selectedColor = !!this.activityBorderColor ? activityBorderColor : 'blue';
    const cssClass = !!this.selectedActivities[activity.activityId] ? `elsa-border-${selectedColor}-600` : `elsa-border-${activityBorderColor}-200 hover:elsa-border-${selectedColor}-600`;
    const displayName = displayContext != undefined ? displayContext.displayName : activity.displayName;
    const typeName = activity.type;

    return `<div class="elsa-border-2 elsa-border-solid ${cssClass}">
      <div class="elsa-p-2 elsa-pr-5">
        <div class="elsa-flex elsa-justify-between elsa-space-x-4 mr-4">
          <div class="elsa-flex-shrink-0">
            ${displayContext?.activityIcon || ''}
          </div>
          <div class="elsa-flex-1 elsa-font-medium elsa-leading-8 elsa-overflow-hidden">
            <p class="elsa-overflow-ellipsis elsa-text-base">${displayName}</p>
            ${typeName !== displayName ? `<p class="elsa-text-gray-400 elsa-text-sm">${typeName}</p>` : ''}
          </div>
        </div>
        ${this.renderActivityBody(displayContext)}
      </div>
    </div>`;
  }

  renderActivityBody(displayContext: ActivityDesignDisplayContext) {
    if (displayContext && displayContext.expanded) {
      return (
        `<div class="elsa-border-t elsa-border-t-solid">
          <div class="elsa-p-4 elsa-text-gray-400 elsa-text-sm">
            <div class="elsa-mb-2">${!!displayContext?.bodyDisplay ? displayContext.bodyDisplay : ''}</div>
            <div>
              <span class="elsa-inline-flex elsa-items-center elsa-px-2.5 elsa-py-0.5 elsa-rounded-full elsa-text-xs elsa-font-medium elsa-bg-gray-100 elsa-text-gray-500">
                <svg class="-elsa-ml-0.5 elsa-mr-1.5 elsa-h-2 elsa-w-2 elsa-text-gray-400" fill="currentColor" viewBox="0 0 8 8">
                  <circle cx="4" cy="4" r="3" />
                </svg>
                ${displayContext != undefined ? displayContext.activityModel.activityId : ''}
              </span>
            </div>
          </div>
        </div>`
      );
    }

    return '';
  }


  render() {

    return (
      <Host>
        {this.mode == WorkflowDesignerMode.Edit && <button type="button" onClick={ e => this.onAddActivity(e)} class="start-btn elsa-absolute elsa-z-1 elsa-h-12 elsa-px-6 elsa-border elsa-border-transparent elsa-text-base elsa-font-medium elsa-rounded-md elsa-text-white elsa-bg-green-600 hover:elsa-bg-green-500 focus:elsa-outline-none focus:elsa-border-green-700 focus:elsa-shadow-outline-green active:elsa-bg-green-700 elsa-transition elsa-ease-in-out elsa-duration-150 elsa-translate-x--1/2 elsa-top-8">Add activity</button>}
        <div class="workflow-canvas elsa-flex-1 elsa-flex" ref={el => (this.el = el)}>
          {this.mode == WorkflowDesignerMode.Test ?
            <div>
              <div id="left" style={{border:`4px solid orange`, position:`fixed`, height: `calc(100vh - 64px)`, width:`4px`, top:`64`, bottom:`0`, left:`0`}}/>
              <div id="right" style={{border:`4px solid orange`, position:`fixed`, height: `calc(100vh - 64px)`, width:`4px`, top:`64`, bottom:`0`, right:`0`}}/>
              <div id="top" style={{border:`4px solid orange`, position:`fixed`, height:`4px`, left:`0`, right:`0`, top:`30`}}/>
              <div id="bottom" style={{border:`4px solid orange`, position:`fixed`, height:`4px`, left:`0`, right:`0`, bottom:`0`}}/>
            </div>
            :
            undefined
          }

          <div ref={el => (this.container = el)}></div>
        </div>
      </Host>
    );
  }

  private getLayoutDirection = () => {
    switch (this.layoutDirection) {
      case LayoutDirection.BottomTop:
        return 'BT';
      case LayoutDirection.LeftRight:
        return 'LR';
      case LayoutDirection.RightLeft:
        return 'RL';
      case LayoutDirection.TopBottom:
      default:
        return 'TB';
    }
  }
}
