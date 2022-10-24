import {Component, Event, EventEmitter, h, Host, Method, Prop, State, Watch, Listen } from '@stencil/core';
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
  ActivityDefinitions,

} from '../../../models';
import {eventBus} from '../../../services';
import state from '../../../utils/store';
import {ActivityIcon} from '../../icons/activity-icon';
import {ActivityContextMenuState, LayoutDirection, WorkflowDesignerMode, LayoutState} from "../tree/elsa-designer-tree/models";

import dagre from "dagre";
import {JsPlumbUtils} from "./jsplumb-utils";
import {
  Connection as JsPlumbConnection,
  DragEventCallbackOptions,
  Endpoint,
  EndpointOptions,
  jsPlumbInstance
} from "jsplumb";

@Component({
  tag: 'elsa-designer',
  styleUrls: ['elsa-designer.css'],
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

  @State() layoutState: LayoutState = {
    dragging: false,
    nodeDragging: false,
    ratio: 0.25,
    scale: 1,
    left: 0,
    top: 0,
    initX: 0,
    initY: 0
  };

  @Prop({ mutable: true })
  workflow: WorkflowModel = {
    activities: [],
    connections: []
  };

  @Prop() copiedActivity: any;

  el: HTMLElement;
  parentActivityId?: string;
  parentActivityOutcome?: string;
  addingActivity: boolean = false;
  activityDisplayContexts: Map<ActivityDesignDisplayContext> = null;
  oldActivityDisplayContexts: Map<ActivityDesignDisplayContext> = null;
  selectedActivities: Map<ActivityModel> = {};
  ignoreCopyPasteActivities: boolean = false;
  jsPlumb: jsPlumbInstance;
  private nodePrevCoordinates: { top: number; left: number };

// Ctrl+C, Ctrl+V Copy/Paste activity
  @Listen('keydown', {target: 'window'})
  async handleKeyDown(e: KeyboardEvent) {

    if(this.mode == WorkflowDesignerMode.Edit) {

      if ((e.ctrlKey || e.metaKey) && e.key === 'c') {
        let activitiesList: Array<any> = [];
        const activityDescriptors: Array<ActivityDescriptor> = state.activityDescriptors;

        Object.keys(this.selectedActivities).forEach((key) => {
          if(key) {
            const activity = this.selectedActivities[key];
            const position = {left: activity.left, top: activity.top};
            const activityDescriptor = activityDescriptors.find(descriptor => descriptor.type === activity.type);
            const newActivity = { activityDescriptor, position };
            activitiesList.push(newActivity);
          }
        })

        if (activitiesList.length > 0) {
          this.copiedActivity = activitiesList[0];
        };
      }
      if ((e.ctrlKey || e.metaKey) && e.key === 'v') {

        if(this.copiedActivity.activityDescriptor) {

          const { activityDescriptor, position } = this.copiedActivity
          let newActivity = {
            activityId: uuid(),
            type: activityDescriptor.type,
            outcomes: activityDescriptor.outcomes,
            displayName: activityDescriptor.displayName,
            properties: [],
            propertyStorageProviders: {},
            left: position.left + 24 || 0,
            top: position.top + 32 || 0
          };

          for (const property of activityDescriptor.inputProperties) {
            newActivity.properties[property.name] = {
              syntax: '',
              expression: '',
            };
          };

          this.addActivity(newActivity, null, null, this.parentActivityOutcome);
        }
      }
    }
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

  componentWillLoad() {
    this.workflowModel = this.model;
    this.jsPlumb = JsPlumbUtils.createInstance('.workflow-canvas');
  }

  async componentDidLoad() {
    let nodes: Array<ActivityModel>  = [];
    const workflowModel = {...this.workflowModel};
    const edges = workflowModel.connections;

    if (this.mode === WorkflowDesignerMode.Blueprint) {
      let newActivities: Array<ActivityModel> = [];

      this.workflowModel.activities.forEach(activity => {
        let outcomes = activity.outcomes;
        const existingSources = edges.filter(x => x.sourceId === activity.activityId);

        if (existingSources && existingSources.length > 0) {

          existingSources.forEach(source => {
            const existingOutcome = outcomes.find(outcome => outcome === source.outcome)

            if (existingOutcome) return
            outcomes.push(source.outcome)
          })
        }

        activity.outcomes = outcomes
        activity.top = 48
        activity.left = 224

        newActivities.push(activity)
      })


      nodes = newActivities;
      this.workflowModel.activities = newActivities;

    } else  nodes = this.workflowModel.activities;

    this.getLayout(nodes, edges);
  }

  componentDidRender() {
    this.setupJsPlumb();
  }

  private elem = (): HTMLElement => this.el;

  private setupJsPlumb = () => {
    this.jsPlumb.reset();
    this.setupJsPlumbEventHandlers();
    this.jsPlumb.batch(() => {
      this.getActivityElements().forEach(this.setupActivityElement);
      this.setupConnections();
    });
    this.jsPlumb.repaintEverything();
  };

  private setupJsPlumbEventHandlers = () => {
    this.jsPlumb.bind('connection', this.connectionCreated);
    this.jsPlumb.bind('connectionDetached', this.connectionDetached);
  };

  private getActivityElements(): NodeListOf<HTMLElement> {
    return this.elem().querySelectorAll(".activity");
  }

  private setupActivityElement = (element: Element) => {
    this.setupDragDrop(element);
    this.setupTargets(element);
    this.setupOutcomes(element);
    this.jsPlumb.revalidate(element);
  };

  private setupConnections = () => {

    for (let connection of  this.workflowModel.connections) {
      const sourceEndpointId: string = JsPlumbUtils.createEndpointUuid(connection.sourceId, connection.outcome);
      const sourceEndpoint: Endpoint = this.jsPlumb.getEndpoint(sourceEndpointId);
      const destinationElementId: string = `wf-activity-${connection.targetId}`;

      this.jsPlumb.connect({
        source: sourceEndpoint,
        target: destinationElementId
      });
    }
  };

  private connectionCreated = async (info: any) => {

    const workflowModel = {...this.workflowModel};
    const connection: JsPlumbConnection = info.connection;
    const sourceEndpoint: any = info.sourceEndpoint;
    const outcome: string = sourceEndpoint.getParameter('outcome');
    const label: any = connection.getOverlay('label');

    label.setLabel(outcome);

    // Check if we already have this connection.
    const sourceActivity: ActivityModel = this.findActivityByElement(info.source);
    const destinationActivity: ActivityModel = this.findActivityByElement(info.target);

    if (!this.enableMultipleConnectionsFromSingleSource) {
      const existingConnection = workflowModel.connections.find(x => x.sourceId === sourceActivity.activityId && x.targetId == destinationActivity.activityId);

      if (!existingConnection) {
        // Add created connection to list.
        const newConnection: ConnectionModel = {
          sourceId: sourceActivity.activityId,
          targetId: destinationActivity.activityId,
          outcome: outcome
        };

        workflowModel.connections = [...workflowModel.connections, newConnection];
        this.updateWorkflowModel(workflowModel);
      }
    }
  };

  private connectionDetached = async (info: any) => {
    let workflowModel = {...this.workflowModel};
    const sourceEndpoint: any = info.sourceEndpoint;
    const outcome: string = sourceEndpoint.getParameter('outcome');
    const sourceActivity: ActivityModel = this.findActivityByElement(info.source);
    const destinationActivity: ActivityModel = this.findActivityByElement(info.target);
    workflowModel = removeConnection(workflowModel, sourceActivity.activityId, outcome);
    this.updateWorkflowModel(workflowModel);
  };

  private setupDragDrop = (element: Element) => {
    let dragStart: any = null;
    let hasDragged: boolean = false;

    this.jsPlumb.draggable(element, {
      containment: "true",
      start: (params: DragEventCallbackOptions) => {
        dragStart = { left: params.e.screenX, top: params.e.screenY };
      },
      stop: async (params: DragEventCallbackOptions) => {
        hasDragged = dragStart.left !== params.e.screenX || dragStart.top !== params.e.screenY;

        if (!hasDragged)
          return;

        const activity = { ...this.findActivityByElement(element) };
        activity.left = params.pos[0];
        activity.top = params.pos[1];

        this.updateActivity(activity);
      }
    });
  };

  private setupTargets(element: Element) {
    this.jsPlumb.makeTarget(element, {
      dropOptions: { hoverClass: 'hover' },
      anchor: 'Continuous',
      endpoint: ['Blank', { radius: 4 }]
    });
  }

  private setupOutcomes(element: Element) {
    const activity = this.findActivityByElement(element);
    const activityDefinitions: Array<ActivityDescriptor> = state.activityDescriptors;
    const definition = activityDefinitions.find(x => x.type == activity.type);
    const outcomes: Array<string> = this.mode === WorkflowDesignerMode.Blueprint ? activity.outcomes : this.getOutcomes(activity, definition);

    for (let outcome of outcomes) {
      const sourceEndpointOptions: EndpointOptions = JsPlumbUtils.getSourceEndpointOptions(activity.activityId, outcome);
      const endpointOptions: any = {
        connectorOverlays: [['Label', { label: outcome, cssClass: 'connection-label' }]],
        overlays:[[ 'Label', { label: outcome, cssClass: 'port-label', location: [0.5, -0.5] }]]
      };

      this.jsPlumb.addEndpoint(element, endpointOptions, sourceEndpointOptions);

    }
  }

  getOutcomes = (activity: ActivityModel, definition: ActivityDefinitions): Array<string> => {
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

  private findActivityByElement = (element: Element): ActivityModel => {
    const id =  ElsaWorkflowDesigner.getActivityId(element);
    return this.findActivityById(id);
  };

  private static getActivityId(element: Element): string {
    return element.attributes['data-activity-id'].value;
  }

  private findActivityById = (id: string): ActivityModel => this.workflowModel.activities.find(x => x.activityId === id);


  private getLayout = (nodes: any, edges: any) => {
    const graph = new dagre.graphlib.Graph();
    graph.setGraph({
      marginx:
        ((document.documentElement.clientWidth || document.body.clientWidth) -
          224 -
          50 -
          240) /
        2,
      marginy: 0,
      nodesep: 30,
      rankdir: this.getLayoutDirection(),
      ranker: "longest-path",
      ranksep: 30
    });
    graph.setDefaultEdgeLabel(() => ({}));

    nodes.forEach((node: any) => {

      graph.setNode(node.activityId, { shape: "rect" });
    });

    edges.forEach((connection: any) => {
      graph.setEdge(connection.sourceId, connection.targetId);
    });

    dagre.layout(graph);
    return graph;
  };

  async componentWillRender() {
    if (!!this.activityDisplayContexts)
      return;

    const activityModels = this.workflowModel.activities;
    const displayContexts: Map<ActivityDesignDisplayContext> = {};

    for (const model of activityModels)
      displayContexts[model.activityId] = await this.getActivityDisplayContext(model);

    this.activityDisplayContexts = displayContexts;
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
    const bodyDisplay = bodyText ? bodyText : undefined;
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
    this.activityDisplayContexts = null;

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
      propertyStorageProviders: {}
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

  updateActivity(activity: ActivityModel) {
    let workflowModel = {...this.workflowModel};
    const activities = [...workflowModel.activities];
    const index = activities.findIndex(x => x.activityId === activity.activityId);
    activities[index] = activity;
    this.updateWorkflowModel({...workflowModel, activities: activities});
  }

  async showActivityPicker() {
    await eventBus.emit(EventTypes.ShowActivityPicker);
  }

  connectedCallback() {
    eventBus.on(EventTypes.ActivityPicked, this.onActivityPicked);
    eventBus.on(EventTypes.UpdateActivity, this.onUpdateActivity);
    //eventBus.on(EventTypes.PasteActivity, this.onPasteActivity);
    // eventBus.on(EventTypes.HideModalDialog, this.onCopyPasteActivityEnabled);
    // eventBus.on(EventTypes.ShowWorkflowSettings, this.onCopyPasteActivityDisabled);
    // eventBus.on(EventTypes.WorkflowExecuted, this.onWorkflowExecuted);
  }

  disconnectedCallback() {
    eventBus.detach(EventTypes.ActivityPicked, this.onActivityPicked);
    eventBus.detach(EventTypes.UpdateActivity, this.onUpdateActivity);
    //eventBus.detach(EventTypes.PasteActivity, this.onPasteActivity);
    // eventBus.detach(EventTypes.HideModalDialog, this.onCopyPasteActivityEnabled);
    // eventBus.detach(EventTypes.ShowWorkflowSettings, this.onCopyPasteActivityDisabled);
    // eventBus.detach(EventTypes.WorkflowExecuted, this.onWorkflowExecuted);
    this.jsPlumb.reset();
  }

  onActivityPicked = async args => {
    const activityDescriptor = args as ActivityDescriptor;
    const activityModel = this.newActivity(activityDescriptor);
    this.addingActivity = true;
    await this.showActivityEditorInternal(activityModel, false);
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

  renderActivity(activity: ActivityModel) {
    const activityDisplayContexts = this.activityDisplayContexts || {};
    const displayContext = activityDisplayContexts[activity.activityId] || undefined;
    const activityContextMenuButton = !!this.activityContextMenuButton ? this.activityContextMenuButton(activity) : '';
    const activityBorderColor = !!this.activityBorderColor ? this.activityBorderColor(activity) : 'gray';
    const selectedColor = !!this.activityBorderColor ? activityBorderColor : 'blue';
    const cssClass = !!this.selectedActivities[activity.activityId] ? `elsa-border-${selectedColor}-600` : `elsa-border-${activityBorderColor}-200 hover:elsa-border-${selectedColor}-600`;
    const customName = displayContext.activityModel.name;
    const displayName = displayContext != undefined ? (customName || displayContext.displayName) : activity.displayName;
    const typeName = activity.type;
    const expanded = displayContext && displayContext.expanded;

    return (
    <div class={`activity-body elsa-border-2 elsa-border-solid elsa-rounded elsa-bg-white elsa-text-left elsa-text-black elsa-text-lg elsa-select-none elsa-max-w-md elsa-shadow-sm elsa-relative ${cssClass} elsa-min-h-12`}>
      <div class="elsa-min-h-12">
        <div class="elsa-flex elsa-justify-between elsa-items-center elsa-space-x-4 elsa-h-12">
          <div class="elsa-flex-shrink-0 elsa-bg-sky-50">
            {displayContext?.activityIcon ? <span innerHTML={displayContext.activityIcon} /> : ''}
          </div>
          <div class="elsa-flex-auto elsa-font-medium elsa-leading-8 elsa-overflow-hidden">
            <p class="elsa-overflow-ellipsis elsa-text-base">{displayName}</p>
            {typeName !== displayName ? <p class="elsa-text-gray-400 elsa-text-sm">{typeName}</p> : ''}
          </div>
          <div class="elsa-mt-1">
            <div class="context-menu-button-container" onClick={(e) => this.onToggleMenu(e, activity)}>
              {activityContextMenuButton}
            </div>
            <button onClick={(e) => this.onToggleContextMenu(e)} data-activity={activity.activityId} type="button" class="expand elsa-ml-1 elsa-text-gray-400 elsa-rounded-full elsa-bg-transparent hover:elsa-text-gray-500 focus:elsa-outline-none focus:elsa-text-gray-500 focus:elsa-bg-gray-100 elsa-transition elsa-ease-in-out elsa-duration-150">
              {!expanded ? <svg xmlns="http://www.w3.org/2000/svg" class="elsa-w-6 elsa-h-6 elsa-text-gray-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
              </svg> : ''}
              {expanded ? <svg xmlns="http://www.w3.org/2000/svg" class="elsa-h-6 elsa-w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 15l7-7 7 7" />
              </svg> : ''}
            </button>
          </div>
        </div>
        {this.renderActivityBody(displayContext)}
        </div>
      </div>
     );
  }

  renderActivityBody(displayContext: ActivityDesignDisplayContext) {
    if (displayContext && displayContext.expanded) {

      return (
        <div class="elsa-border-t elsa-border-t-solid">
          <div class="elsa-p-4 elsa-text-gray-400 elsa-text-sm">
            <div class="elsa-mb-2 elsa-break-words">{!!displayContext?.bodyDisplay ? displayContext.bodyDisplay : ''}</div>
            <div>
              <span class="elsa-inline-flex elsa-items-center elsa-px-2.5 elsa-py-0.5 elsa-rounded-full elsa-text-xs elsa-font-medium elsa-bg-gray-100 elsa-text-gray-500">
                <svg class="-elsa-ml-0.5 elsa-mr-1.5 elsa-h-2 elsa-w-2 elsa-text-gray-400" fill="currentColor" viewBox="0 0 8 8">
                  <circle cx="4" cy="4" r="3" />
                </svg>
                {displayContext != undefined ? displayContext.activityModel.activityId : ''}
              </span>
            </div>
          </div>
      </div>
      );
    }
    return '';
  }

  onToggleContextMenu(e: Event) {
    const target = e.target as HTMLElement;
    const activityId = target.dataset.activity;
    const activityContext = this.activityDisplayContexts[activityId];

    if (activityContext) {
      activityContext.expanded = !activityContext.expanded;
    }
  }

  onToggleMenu(e: MouseEvent, activity: ActivityModel) {

    if (this.mode == WorkflowDesignerMode.Edit || this.mode == WorkflowDesignerMode.Instance) {
      e.stopPropagation();
      this.handleContextMenuChange({
        x: e.clientX,
        y: e.clientY,
        shown: true,
        activity: activity,
        selectedActivities: this.selectedActivities
      });
    }

    else if (this.mode == WorkflowDesignerMode.Test) {
      e.stopPropagation();
      this.handleContextMenuTestChange({
        x: e.clientX,
        y: e.clientY,
        shown: true,
        activity: activity
      });
    }
  }

  getActivitiesList() {
    let newList = []
    Object.entries(this.activityDisplayContexts).forEach(([key, value]) => newList.push(value))
    return newList
  }

  onClickActivityBody(e: MouseEvent, activity: ActivityModel) {
    const {activityId} = activity;

    if (this.mode == WorkflowDesignerMode.Edit && this.parentActivityId && this.parentActivityOutcome) {
      this.addConnection(this.parentActivityId, activityId, this.parentActivityOutcome);
    }
    else {
      // When clicking an activity with shift:
      if (e.shiftKey) {
        if (!!this.selectedActivities[activityId])
          delete this.selectedActivities[activityId];
        else {
          this.selectedActivities[activityId] = activity;
          this.activitySelected.emit(activity);
        }
      }
      // When clicking an activity:
      else {
        if (!!this.selectedActivities[activityId])
          delete this.selectedActivities[activityId];
        else {
          for (const key in this.selectedActivities) {
            this.activityDeselected.emit(this.selectedActivities[key]);
          }
          this.selectedActivities = {};
          this.selectedActivities[activityId] = activity;
          this.activitySelected.emit(activity);

        }
      }
    }
  }

  async onAddActivity(e: Event) {
    e.preventDefault();
    if (this.mode !== WorkflowDesignerMode.Test) await this.showActivityPicker();
  }

  onCanvasMousewheel = (e: WheelEvent) => {
    if (e.deltaY < 0) {

      this.layoutState = {
        dragging: this.layoutState.dragging,
        nodeDragging: true,
        ratio:  this.layoutState.ratio,
        scale:  this.layoutState.scale + this.layoutState.scale * this.layoutState.ratio,
        left:  this.layoutState.left,
        top:  this.layoutState.top,
        initX:  this.layoutState.initX,
        initY:  this.layoutState.initY
      }
    }

    if (e.deltaY > 0) {
      this.layoutState = {
        dragging: this.layoutState.dragging,
        nodeDragging: true,
        ratio:  this.layoutState.ratio,
        scale:  this.layoutState.scale - this.layoutState.scale * this.layoutState.ratio,
        left:  this.layoutState.left,
        top:  this.layoutState.top,
        initX:  this.layoutState.initX,
        initY:  this.layoutState.initY
      }
    }
  };

  onCanvasMouseMove = (e: MouseEvent) => {
    if (!this.layoutState.dragging) {
      return;
    }
    this.el.style.left = this.layoutState.left + e.pageX -this.layoutState.initX + "px";
    this.el.style.top = this.layoutState.top + e.pageY - this.layoutState.initY + "px";
  };

  onMouseMove = (e: MouseEvent) => {
    if (!this.layoutState.nodeDragging) {
      this.layoutState = {
        dragging: this.layoutState.dragging,
        nodeDragging: true,
        ratio:  this.layoutState.ratio,
        scale:  this.layoutState.scale,
        left:  this.layoutState.left,
        top:  this.layoutState.top,
        initX:  this.layoutState.initX,
        initY:  this.layoutState.initY
      };
    }
  };

  onCanvasMouseDown = (e: MouseEvent) => {
    this.layoutState = {
      dragging: true,
      nodeDragging: this.layoutState.nodeDragging,
      ratio:  this.layoutState.ratio,
      scale:  this.layoutState.scale,
      left:  this.layoutState.left,
      top:  this.layoutState.top,
      initX:  e.pageX,
      initY:  e.pageY
    };
  };

  onCanvasMouseUpLeave = (e: MouseEvent) => {
    if (this.layoutState.dragging) {
      let _left = this.layoutState.left + e.pageX - this.layoutState.initX;
      let _top = this.layoutState.top + e.pageY - this.layoutState.initY;

     this.el.style.left = _left + "px";
     this.el.style.top = _top + "px";

    this.layoutState = {
      dragging: false,
      nodeDragging: false,
      ratio: this.layoutState.ratio,
      scale: this.layoutState.scale,
      left: _left,
      top:  _top,
      initX:  e.pageX,
      initY:  e.pageY
    };
    }
  };

  render() {
    const activitiesList = this.getActivitiesList();

    let leftArray: Array<number | any> = [];
    let topArray: Array<number | any> = [];

    leftArray.length > 0 && leftArray.sort((a, b): any => {
      if(a && b) return  a > b
    });

    topArray.length > 0 && topArray.sort((a, b): any =>  {
      if(a && b) return  a > b
    });

    let difLeft = leftArray.length > 0 ? leftArray[leftArray.length - 1] - leftArray[0] + 240 : 1;
    let difTop = topArray.length > 0 ? topArray[topArray.length - 1] - topArray[0] + 80 : 1;

    let scale = Math.min(144 / difLeft, 144 / difTop);

    let left = 0;
    let top = 0;

    if (difLeft > difTop) {
      left = -leftArray[0] * scale;
      top = -topArray[0] * scale + (144 - difTop * scale) / 2;
    } else {
      left = -leftArray[0] * scale + (144 - difLeft * scale) / 2;
      top = -topArray[0] * scale;
    }

    let translateWidth =
      (document.documentElement.clientWidth * (1 - this.layoutState.scale)) / 2;
    let translateHeight =
      ((document.documentElement.clientHeight - 64) * (1 - this.layoutState.scale)) / 2;

    this.jsPlumb.repaintEverything();

    return (
      <Host
        onWheel={this.onCanvasMousewheel}
        onMouseMove={this.onCanvasMouseMove}
        onMouseDown={this.onCanvasMouseDown}
        onMouseUp={this.onCanvasMouseUpLeave}
        onMouseLeave={this.onCanvasMouseUpLeave}
      >
        {this.mode == WorkflowDesignerMode.Edit && <button onClick={ e => this.onAddActivity(e)} class="elsa-absolute elsa-z-1 elsa-h-12 elsa-px-6 elsa-border elsa-border-transparent elsa-text-base elsa-font-medium elsa-rounded-md elsa-text-white elsa-bg-green-600 hover:elsa-bg-green-500 focus:elsa-outline-none focus:elsa-border-green-700 focus:elsa-shadow-outline-green active:elsa-bg-green-700 elsa-transition elsa-ease-in-out elsa-duration-150 elsa-translate-x--1/2 elsa-top-8 elsa-right-1/2 ">Add Activity</button>}
        <div class="workflow-canvas elsa-relative" id="workflow-canvas" ref={el => (this.el = el)} style={{
            transformOrigin: "0px 0px 0px",
            transform: `translate(${translateWidth}px, ${translateHeight}px) scale(${this.layoutState.scale})`
          }}>
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
          {activitiesList.map((model) => {
            const activity = model.activityModel;

            leftArray.push(activity.left ? parseFloat(activity.left) : 0);
            topArray.push(activity.top ? parseFloat(activity.top) : 0);

              return (
                <div id={`wf-activity-${activity.activityId}`}
                  data-activity-id={activity.activityId} class="activity"
                  style={{'left': `${activity.left}px`, 'top': `${activity.top}px`}}
                  onClick={e => this.onClickActivityBody(e, activity)}
                  onMouseDown={e => this.onNodeMouseDown(e)}
                  onMouseUp={e => this.onNodeMouseUp(e, activity)}
                >
                  {this.renderActivity(activity)}
                </div>);
            })
          }
        </div>
      </Host>
    );
  }

  onNodeMouseDown(e: MouseEvent): void {
    this.nodePrevCoordinates = {top: e.clientY, left: e.clientX};
  }

  onNodeMouseUp(e: MouseEvent, activity: ActivityModel): void {
    if (activity == null) {
      this.nodePrevCoordinates = null;
      return;
    }
    let nodeStartingCoordinates = this.nodePrevCoordinates;
    let dx = e.clientX - nodeStartingCoordinates.left;
    let dy = e.clientY - nodeStartingCoordinates.top;
    activity.left = (activity.left == null ? 0 : activity.left) + dx;
    activity.top = (activity.top == null ? 0 : activity.top) + dy;
    this.nodePrevCoordinates = null;
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
