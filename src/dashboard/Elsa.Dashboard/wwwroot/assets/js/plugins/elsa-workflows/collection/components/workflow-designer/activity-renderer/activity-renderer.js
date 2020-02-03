import { h, Host } from "@stencil/core";
import { ActivityDisplayMode } from "../../../models";
import ActivityManager from '../../../services/activity-manager';
import DisplayManager from '../../../services/display-manager';
export class ActivityRenderer {
    constructor() {
        this.displayMode = ActivityDisplayMode.Design;
    }
    render() {
        if (!this.activity || !this.activityDefinition)
            return null;
        switch (this.displayMode) {
            case ActivityDisplayMode.Design:
                return this.renderDesigner();
            case ActivityDisplayMode.Edit:
                return this.renderEditor();
        }
    }
    renderDesigner() {
        const activity = this.activity;
        const definition = this.activityDefinition;
        const result = ActivityManager.renderDesigner(activity, definition);
        const iconClass = `${result.icon} mr-1`;
        return (h("div", null,
            h("h5", null,
                h("i", { class: iconClass }),
                result.title),
            h("p", { innerHTML: result.description })));
    }
    renderEditor() {
        const activity = this.activity;
        const definition = this.activityDefinition;
        const properties = definition.properties;
        return (h(Host, null, properties.map(property => {
            const html = DisplayManager.displayEditor(activity, property);
            return h("div", { class: "form-group", innerHTML: html });
        })));
    }
    async updateEditor(formData) {
        const activity = Object.assign({}, this.activity);
        const definition = this.activityDefinition;
        const properties = definition.properties;
        for (const property of properties) {
            DisplayManager.updateEditor(activity, property, formData);
        }
        return activity;
    }
    static get is() { return "wf-activity-renderer"; }
    static get originalStyleUrls() { return {
        "$": ["activity-renderer.scss"]
    }; }
    static get styleUrls() { return {
        "$": ["activity-renderer.css"]
    }; }
    static get properties() { return {
        "activityDefinition": {
            "type": "unknown",
            "mutable": false,
            "complexType": {
                "original": "ActivityDefinition",
                "resolved": "ActivityDefinition",
                "references": {
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
            }
        },
        "activity": {
            "type": "unknown",
            "mutable": false,
            "complexType": {
                "original": "Activity",
                "resolved": "Activity",
                "references": {
                    "Activity": {
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
        "displayMode": {
            "type": "string",
            "mutable": false,
            "complexType": {
                "original": "ActivityDisplayMode",
                "resolved": "ActivityDisplayMode.Design | ActivityDisplayMode.Edit",
                "references": {
                    "ActivityDisplayMode": {
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
            "attribute": "display-mode",
            "reflect": false,
            "defaultValue": "ActivityDisplayMode.Design"
        }
    }; }
    static get methods() { return {
        "updateEditor": {
            "complexType": {
                "signature": "(formData: FormData) => Promise<Activity>",
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
                    },
                    "FormData": {
                        "location": "global"
                    }
                },
                "return": "Promise<Activity>"
            },
            "docs": {
                "text": "",
                "tags": []
            }
        }
    }; }
}
