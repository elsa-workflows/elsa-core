import { h } from "@stencil/core";
import { JsPlumbUtils } from "./jsplumb-utils";
import uuid from 'uuid-browser/v4';
import { ActivityDisplayMode } from "../../../models";
import ActivityManager from '../../../services/activity-manager';
import { deepClone } from "../../../utils/deep-clone";
import dagre from 'dagre';
export class Designer {
    constructor() {
        this.activityDefinitions = [];
        this.workflow = {
            activities: [],
            connections: []
        };
        this.lastClickedLocation = null;
        this.elem = () => this.el;
        this.renderContextMenu = () => {
            if (this.readonly)
                return null;
            return ([
                h("wf-context-menu", { target: this.elem() },
                    h("wf-context-menu-item", { text: "Auto Layout", onClick: this.onAutoLayoutClick }),
                    h("wf-context-menu-item", { text: "Add Activity", onClick: this.onAddActivityClick })),
                h("wf-context-menu", { ref: (el) => this.activityContextMenu = el },
                    h("wf-context-menu-item", { text: "Edit", onClick: this.onEditActivityClick }),
                    h("wf-context-menu-item", { text: "Delete", onClick: this.onDeleteActivityClick }))
            ]);
        };
        this.deleteActivity = async (activity) => {
            const activities = this.workflow.activities.filter(x => x.id !== activity.id);
            const connections = this.workflow.connections.filter(x => x.sourceActivityId != activity.id && x.destinationActivityId != activity.id);
            this.workflow = Object.assign({}, this.workflow, { activities, connections });
        };
        this.setupJsPlumb = () => {
            this.jsPlumb.reset();
            this.setupJsPlumbEventHandlers();
            this.jsPlumb.batch(() => {
                this.getActivityElements().forEach(this.setupActivityElement);
                this.setupConnections();
            });
            this.jsPlumb.repaintEverything();
        };
        this.setupPopovers = () => {
            $('[data-toggle="popover"]').popover({});
        };
        this.setupActivityElement = (element) => {
            if (!this.readonly) {
                this.setupDragDrop(element);
            }
            this.setupTargets(element);
            this.setupOutcomes(element);
            this.jsPlumb.revalidate(element);
        };
        this.setupDragDrop = (element) => {
            let dragStart = null;
            let hasDragged = false;
            this.jsPlumb.draggable(element, {
                containment: "true",
                start: (params) => {
                    dragStart = { left: params.e.screenX, top: params.e.screenY };
                },
                stop: async (params) => {
                    hasDragged = dragStart.left !== params.e.screenX || dragStart.top !== params.e.screenY;
                    if (!hasDragged)
                        return;
                    const activity = Object.assign({}, this.findActivityByElement(element));
                    activity.left = params.pos[0];
                    activity.top = params.pos[1];
                    await this.updateActivityInternal(activity);
                }
            });
        };
        this.setupConnections = () => {
            for (let connection of this.workflow.connections) {
                const sourceEndpointId = JsPlumbUtils.createEndpointUuid(connection.sourceActivityId, connection.outcome);
                const sourceEndpoint = this.jsPlumb.getEndpoint(sourceEndpointId);
                const destinationElementId = `wf-activity-${connection.destinationActivityId}`;
                this.jsPlumb.connect({
                    source: sourceEndpoint,
                    target: destinationElementId
                });
            }
        };
        this.findActivityByElement = (element) => {
            const id = Designer.getActivityId(element);
            return this.findActivityById(id);
        };
        this.findActivityById = (id) => this.workflow.activities.find(x => x.id === id);
        this.updateActivityInternal = async (activity) => {
            const activities = [...this.workflow.activities];
            const index = activities.findIndex(x => x.id == activity.id);
            activities[index] = Object.assign({}, activity);
            this.workflow = Object.assign({}, this.workflow, { activities });
        };
        this.setupJsPlumbEventHandlers = () => {
            this.jsPlumb.bind('connection', this.connectionCreated);
            this.jsPlumb.bind('connectionDetached', this.connectionDetached);
        };
        this.connectionCreated = async (info) => {
            const connection = info.connection;
            const sourceEndpoint = info.sourceEndpoint;
            const outcome = sourceEndpoint.getParameter('outcome');
            const label = connection.getOverlay('label');
            label.setLabel(outcome);
            // Check if we already have this connection.
            const sourceActivity = this.findActivityByElement(info.source);
            const destinationActivity = this.findActivityByElement(info.target);
            const wfConnection = this.workflow.connections.find(x => x.sourceActivityId === sourceActivity.id && x.destinationActivityId == destinationActivity.id);
            if (!wfConnection) {
                // Add created connection to list.
                const connections = [...this.workflow.connections, {
                        sourceActivityId: sourceActivity.id,
                        destinationActivityId: destinationActivity.id,
                        outcome: outcome
                    }];
                this.workflow = Object.assign({}, this.workflow, { connections });
            }
        };
        this.connectionDetached = async (info) => {
            const sourceEndpoint = info.sourceEndpoint;
            const outcome = sourceEndpoint.getParameter('outcome');
            const sourceActivity = this.findActivityByElement(info.source);
            const destinationActivity = this.findActivityByElement(info.target);
            const connections = this.workflow.connections.filter(x => !(x.sourceActivityId === sourceActivity.id && x.destinationActivityId === destinationActivity.id && x.outcome === outcome));
            this.workflow = Object.assign({}, this.workflow, { connections });
        };
        this.onAddActivityClick = (e) => {
            const el = this.elem();
            this.lastClickedLocation = {
                left: e.pageX - el.offsetLeft,
                top: e.pageY - el.offsetTop
            };
            this.addActivityEvent.emit();
        };
        this.onAutoLayoutClick = () => {
            this.autoLayout();
        };
        this.onDeleteActivityClick = async () => {
            await this.deleteActivity(this.selectedActivity);
        };
        this.onEditActivityClick = () => {
            this.onEditActivity(this.selectedActivity);
        };
    }
    onWorkflowChanged(value) {
        this.workflowChanged.emit(deepClone(value));
    }
    async newWorkflow() {
        this.workflow = {
            activities: [],
            connections: []
        };
    }
    async autoLayout() {
        var self = this;
        var g = new dagre.graphlib.Graph();
        var allNodes = document.querySelectorAll("[data-activity-id]");
        g.setGraph({ nodesep: 100, ranksep: 100, marginx: 100, marginy: 100 });
        g.setDefaultEdgeLabel(function () { return {}; });
        allNodes.forEach(function (element) {
            var activityId = element.getAttribute('data-activity-id');
            var boundingClientRect = element.getBoundingClientRect();
            g.setNode(activityId, {
                width: boundingClientRect.width,
                height: boundingClientRect.height
            });
        });
        this.workflow.connections.forEach(function (connection) {
            g.setEdge(connection.sourceActivityId, connection.destinationActivityId);
        });
        dagre.layout(g);
        g.nodes().forEach(function (n) {
            var node = g.node(n);
            var activity = self.findActivityById(n);
            if (activity != undefined) {
                activity.top = node.y - node.height / 2;
                activity.left = node.x - node.width / 2;
                self.updateActivityInternal(activity);
            }
        });
    }
    ;
    async getWorkflow() {
        return deepClone(this.workflow);
    }
    async addActivity(activityDefinition) {
        const left = !!this.lastClickedLocation ? this.lastClickedLocation.left : 150;
        const top = !!this.lastClickedLocation ? this.lastClickedLocation.top : 150;
        const activity = {
            id: uuid(),
            top: top,
            left: left,
            type: activityDefinition.type,
            state: {}
        };
        this.lastClickedLocation = null;
        const activities = [...this.workflow.activities, activity];
        this.workflow = Object.assign({}, this.workflow, { activities });
    }
    async updateActivity(activity) {
        await this.updateActivityInternal(activity);
    }
    componentWillLoad() {
        this.jsPlumb = JsPlumbUtils.createInstance('.workflow-canvas', this.readonly);
    }
    componentDidRender() {
        this.setupJsPlumb();
        this.setupPopovers();
    }
    render() {
        const activities = this.createActivityModels();
        return (h("host", { class: "workflow-canvas", ref: el => this.canvas = el, style: { height: this.canvasHeight } },
            activities.map((model) => {
                const activity = model.activity;
                const styles = { 'left': `${activity.left}px`, 'top': `${activity.top}px` };
                const classes = {
                    'activity': true,
                    'activity-blocking': !!activity.blocking,
                    'activity-executed': !!activity.executed,
                    'activity-faulted': activity.faulted
                };
                if (!this.readonly) {
                    return (h("div", { id: `wf-activity-${activity.id}`, "data-activity-id": activity.id, class: classes, style: styles, onDblClick: () => this.onEditActivity(activity), onContextMenu: (e) => this.onActivityContextMenu(e, activity) },
                        h("wf-activity-renderer", { activity: activity, activityDefinition: model.definition, displayMode: ActivityDisplayMode.Design })));
                }
                else {
                    classes['noselect'] = true;
                    const popoverAttributes = {};
                    if (!!activity.message) {
                        popoverAttributes['data-toggle'] = 'popover';
                        popoverAttributes['data-trigger'] = 'hover';
                        popoverAttributes['title'] = activity.message.title;
                        popoverAttributes['data-content'] = activity.message.content;
                    }
                    return (h("div", Object.assign({ id: `wf-activity-${activity.id}`, "data-activity-id": activity.id, class: classes, style: styles }, popoverAttributes),
                        h("wf-activity-renderer", { activity: activity, activityDefinition: model.definition, displayMode: ActivityDisplayMode.Design })));
                }
            }),
            this.renderContextMenu()));
    }
    createActivityModels() {
        const workflow = this.workflow || {
            activities: [],
            connections: []
        };
        const activityDefinitions = this.activityDefinitions || [];
        return workflow.activities.map((activity) => {
            const definition = activityDefinitions.find(x => x.type == activity.type);
            return {
                activity,
                definition
            };
        });
    }
    setupTargets(element) {
        this.jsPlumb.makeTarget(element, {
            dropOptions: { hoverClass: 'hover' },
            anchor: 'Continuous',
            endpoint: ['Blank', { radius: 4 }]
        });
    }
    setupOutcomes(element) {
        const activity = this.findActivityByElement(element);
        const definition = this.activityDefinitions.find(x => x.type == activity.type);
        const outcomes = ActivityManager.getOutcomes(activity, definition);
        const activityExecuted = activity.executed;
        for (let outcome of outcomes) {
            const sourceEndpointOptions = JsPlumbUtils.getSourceEndpointOptions(activity.id, outcome, activityExecuted);
            const endpointOptions = {
                connectorOverlays: [['Label', { label: outcome, cssClass: 'connection-label' }]],
            };
            this.jsPlumb.addEndpoint(element, endpointOptions, sourceEndpointOptions);
        }
    }
    getActivityElements() {
        return this.elem().querySelectorAll(".activity");
    }
    static getActivityId(element) {
        return element.attributes['data-activity-id'].value;
    }
    onEditActivity(activity) {
        const clone = deepClone(activity);
        this.editActivityEvent.emit(clone);
    }
    async onActivityContextMenu(e, activity) {
        this.selectedActivity = activity;
        await this.activityContextMenu.handleContextMenuEvent(e);
    }
    static get is() { return "wf-designer"; }
    static get originalStyleUrls() { return {
        "$": ["designer.scss"]
    }; }
    static get styleUrls() { return {
        "$": ["designer.css"]
    }; }
    static get properties() { return {
        "canvasHeight": {
            "type": "string",
            "mutable": false,
            "complexType": {
                "original": "string",
                "resolved": "string",
                "references": {}
            },
            "required": false,
            "optional": false,
            "docs": {
                "tags": [],
                "text": ""
            },
            "attribute": "canvas-height",
            "reflect": true
        },
        "activityDefinitions": {
            "type": "unknown",
            "mutable": false,
            "complexType": {
                "original": "Array<ActivityDefinition>",
                "resolved": "ActivityDefinition[]",
                "references": {
                    "Array": {
                        "location": "global"
                    },
                    "ActivityDefinition": {
                        "location": "import",
                        "path": "../../../models"
                    }
                }
            },
            "required": false,
            "optional": false,
            "docs": {
                "tags": [],
                "text": ""
            },
            "defaultValue": "[]"
        },
        "readonly": {
            "type": "boolean",
            "mutable": false,
            "complexType": {
                "original": "boolean",
                "resolved": "boolean",
                "references": {}
            },
            "required": false,
            "optional": false,
            "docs": {
                "tags": [],
                "text": ""
            },
            "attribute": "readonly",
            "reflect": true
        },
        "workflow": {
            "type": "unknown",
            "mutable": true,
            "complexType": {
                "original": "Workflow",
                "resolved": "{ activities: Activity[]; connections: Connection[]; }",
                "references": {
                    "Workflow": {
                        "location": "import",
                        "path": "../../../models"
                    }
                }
            },
            "required": false,
            "optional": false,
            "docs": {
                "tags": [],
                "text": ""
            },
            "defaultValue": "{\r\n    activities: [],\r\n    connections: []\r\n  }"
        }
    }; }
    static get events() { return [{
            "method": "editActivityEvent",
            "name": "edit-activity",
            "bubbles": true,
            "cancelable": true,
            "composed": true,
            "docs": {
                "tags": [],
                "text": ""
            },
            "complexType": {
                "original": "any",
                "resolved": "any",
                "references": {}
            }
        }, {
            "method": "addActivityEvent",
            "name": "add-activity",
            "bubbles": true,
            "cancelable": true,
            "composed": true,
            "docs": {
                "tags": [],
                "text": ""
            },
            "complexType": {
                "original": "any",
                "resolved": "any",
                "references": {}
            }
        }, {
            "method": "workflowChanged",
            "name": "workflowChanged",
            "bubbles": true,
            "cancelable": true,
            "composed": true,
            "docs": {
                "tags": [],
                "text": ""
            },
            "complexType": {
                "original": "any",
                "resolved": "any",
                "references": {}
            }
        }]; }
    static get methods() { return {
        "newWorkflow": {
            "complexType": {
                "signature": "() => Promise<void>",
                "parameters": [],
                "references": {
                    "Promise": {
                        "location": "global"
                    }
                },
                "return": "Promise<void>"
            },
            "docs": {
                "text": "",
                "tags": []
            }
        },
        "autoLayout": {
            "complexType": {
                "signature": "() => Promise<void>",
                "parameters": [],
                "references": {
                    "Promise": {
                        "location": "global"
                    }
                },
                "return": "Promise<void>"
            },
            "docs": {
                "text": "",
                "tags": []
            }
        },
        "getWorkflow": {
            "complexType": {
                "signature": "() => Promise<any>",
                "parameters": [],
                "references": {
                    "Promise": {
                        "location": "global"
                    }
                },
                "return": "Promise<any>"
            },
            "docs": {
                "text": "",
                "tags": []
            }
        },
        "addActivity": {
            "complexType": {
                "signature": "(activityDefinition: ActivityDefinition) => Promise<void>",
                "parameters": [{
                        "tags": [],
                        "text": ""
                    }],
                "references": {
                    "Promise": {
                        "location": "global"
                    },
                    "ActivityDefinition": {
                        "location": "import",
                        "path": "../../../models"
                    },
                    "Activity": {
                        "location": "import",
                        "path": "../../../models"
                    }
                },
                "return": "Promise<void>"
            },
            "docs": {
                "text": "",
                "tags": []
            }
        },
        "updateActivity": {
            "complexType": {
                "signature": "(activity: Activity) => Promise<void>",
                "parameters": [{
                        "tags": [],
                        "text": ""
                    }],
                "references": {
                    "Promise": {
                        "location": "global"
                    },
                    "Activity": {
                        "location": "import",
                        "path": "../../../models"
                    }
                },
                "return": "Promise<void>"
            },
            "docs": {
                "text": "",
                "tags": []
            }
        }
    }; }
    static get elementRef() { return "el"; }
    static get watchers() { return [{
            "propName": "workflow",
            "methodName": "onWorkflowChanged"
        }]; }
}
