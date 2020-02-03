import { h } from "@stencil/core";
import 'dragscroll';
import "../../../drivers";
import DisplayManager from '../../../services/display-manager';
import pluginStore from '../../../services/workflow-plugin-store';
import { deepClone } from "../../../utils/deep-clone";
import '../../../plugins/console-activities';
import '../../../plugins/control-flow-activities';
import '../../../plugins/email-activities';
import '../../../plugins/http-activities';
import '../../../plugins/mass-transit-activities';
import '../../../plugins/primitives-activities';
import '../../../plugins/timer-activities';
import { BooleanFieldDriver, ExpressionFieldDriver, ListFieldDriver, SelectFieldDriver, TextFieldDriver } from "../../../drivers";
export class DesignerHost {
    constructor() {
        this.activityDefinitions = [];
        this.loadActivityDefinitions = () => {
            const pluginsData = this.pluginsData || '';
            const pluginNames = pluginsData.split(/[ ,]+/).map(x => x.trim());
            return pluginStore
                .list()
                .filter(x => pluginNames.indexOf(x.getName()) > -1)
                .filter(x => !!x.getActivityDefinitions)
                .map(x => x.getActivityDefinitions())
                .reduce((a, b) => a.concat(b), []);
        };
        this.onWorkflowChanged = (e) => {
            this.workflowChanged.emit(e.detail);
        };
        this.initActivityDefinitions = () => {
            this.activityDefinitions = this.loadActivityDefinitions();
            if (!!this.activityDefinitionsData) {
                const definitions = JSON.parse(this.activityDefinitionsData);
                this.activityDefinitions = [...this.activityDefinitions, ...definitions];
            }
        };
        this.initFieldDrivers = () => {
            DisplayManager.addDriver('text', new TextFieldDriver());
            DisplayManager.addDriver('expression', new ExpressionFieldDriver());
            DisplayManager.addDriver('list', new ListFieldDriver());
            DisplayManager.addDriver('boolean', new BooleanFieldDriver());
            DisplayManager.addDriver('select', new SelectFieldDriver());
        };
        this.initWorkflow = () => {
            if (!!this.workflowData) {
                const workflow = JSON.parse(this.workflowData);
                if (!workflow.activities)
                    workflow.activities = [];
                if (!workflow.connections)
                    workflow.connections = [];
                this.designer.workflow = workflow;
            }
        };
    }
    async newWorkflow() {
        await this.designer.newWorkflow();
    }
    async autoLayout() {
        await this.designer.autoLayout();
    }
    async getWorkflow() {
        return await this.designer.getWorkflow();
    }
    async showActivityPicker() {
        await this.activityPicker.show();
    }
    async export(formatDescriptor) {
        await this.importExport.export(this.designer, formatDescriptor);
    }
    async import() {
        await this.importExport.import();
    }
    async onActivityPicked(e) {
        await this.designer.addActivity(e.detail);
    }
    async onEditActivity(e) {
        this.activityEditor.activity = e.detail;
        this.activityEditor.show = true;
    }
    async onAddActivity() {
        await this.showActivityPicker();
    }
    async onUpdateActivity(e) {
        await this.designer.updateActivity(e.detail);
    }
    async onExportWorkflow(e) {
        if (!this.importExport)
            return;
        await this.importExport.export(this.designer, e.detail);
    }
    async onImportWorkflow(e) {
        this.designer.workflow = deepClone(e.detail);
    }
    componentWillLoad() {
        this.initActivityDefinitions();
        this.initFieldDrivers();
    }
    componentDidLoad() {
        this.initWorkflow();
    }
    render() {
        const activityDefinitions = this.activityDefinitions;
        return (h("host", null,
            h("wf-activity-picker", { activityDefinitions: activityDefinitions, ref: el => this.activityPicker = el }),
            h("wf-activity-editor", { activityDefinitions: activityDefinitions, ref: el => this.activityEditor = el }),
            h("wf-import-export", { ref: el => this.importExport = el }),
            h("div", { class: "workflow-designer-wrapper dragscroll" },
                h("wf-designer", { activityDefinitions: activityDefinitions, ref: el => this.designer = el, canvasHeight: this.canvasHeight, workflow: this.workflow, readonly: this.readonly, onWorkflowChanged: this.onWorkflowChanged }))));
    }
    static get is() { return "wf-designer-host"; }
    static get originalStyleUrls() { return {
        "$": ["designer-host.scss"]
    }; }
    static get styleUrls() { return {
        "$": ["designer-host.css"]
    }; }
    static get properties() { return {
        "workflow": {
            "type": "unknown",
            "mutable": false,
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
            }
        },
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
        "activityDefinitionsData": {
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
            "attribute": "data-activity-definitions",
            "reflect": false
        },
        "workflowData": {
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
            "attribute": "data-workflow",
            "reflect": false
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
            "reflect": false
        },
        "pluginsData": {
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
            "attribute": "plugins",
            "reflect": false
        }
    }; }
    static get states() { return {
        "activityDefinitions": {}
    }; }
    static get events() { return [{
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
        "showActivityPicker": {
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
        "export": {
            "complexType": {
                "signature": "(formatDescriptor: WorkflowFormatDescriptor) => Promise<void>",
                "parameters": [{
                        "tags": [],
                        "text": ""
                    }],
                "references": {
                    "Promise": {
                        "location": "global"
                    },
                    "WorkflowFormatDescriptor": {
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
        "import": {
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
        }
    }; }
    static get elementRef() { return "el"; }
    static get listeners() { return [{
            "name": "activity-picked",
            "method": "onActivityPicked",
            "target": undefined,
            "capture": false,
            "passive": false
        }, {
            "name": "edit-activity",
            "method": "onEditActivity",
            "target": undefined,
            "capture": false,
            "passive": false
        }, {
            "name": "add-activity",
            "method": "onAddActivity",
            "target": undefined,
            "capture": false,
            "passive": false
        }, {
            "name": "update-activity",
            "method": "onUpdateActivity",
            "target": undefined,
            "capture": false,
            "passive": false
        }, {
            "name": "export-workflow",
            "method": "onExportWorkflow",
            "target": undefined,
            "capture": false,
            "passive": false
        }, {
            "name": "import-workflow",
            "method": "onImportWorkflow",
            "target": undefined,
            "capture": false,
            "passive": false
        }]; }
}
