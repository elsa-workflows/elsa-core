import { h } from "@stencil/core";
import { ActivityDisplayMode } from "../../../models";
export class ActivityEditor {
    constructor() {
        this.activityDefinitions = [];
    }
    async onSubmit(e) {
        e.preventDefault();
        const form = e.target;
        const formData = new FormData(form);
        const updatedActivity = await this.renderer.updateEditor(formData);
        this.submit.emit(updatedActivity);
        this.show = false;
    }
    componentDidRender() {
        const modal = $(this.el.querySelector('.modal'));
        const action = this.show ? 'show' : 'hide';
        modal.modal(action);
    }
    render() {
        const activity = this.activity;
        if (!activity) {
            return null;
        }
        const activityDefinition = this.activityDefinitions.find(x => x.type == activity.type);
        if (!activityDefinition) {
            console.error(`No activity of type ${this.activity.type} exists in the library.`);
            return;
        }
        const displayName = activityDefinition.displayName;
        return (h("div", null,
            h("div", { class: "modal", tabindex: "-1", role: "dialog" },
                h("div", { class: "modal-dialog modal-xl", role: "document" },
                    h("div", { class: "modal-content" },
                        h("form", { onSubmit: e => this.onSubmit(e) },
                            h("div", { class: "modal-header" },
                                h("h5", { class: "modal-title" },
                                    "Edit ",
                                    displayName),
                                h("button", { type: "button", class: "close", "data-dismiss": "modal", "aria-label": "Close" },
                                    h("span", { "aria-hidden": "true" }, "\u00D7"))),
                            h("div", { class: "modal-body" },
                                h("wf-activity-renderer", { activity: activity, activityDefinition: activityDefinition, displayMode: ActivityDisplayMode.Edit, ref: x => this.renderer = x })),
                            h("div", { class: "modal-footer" },
                                h("button", { type: "button", class: "btn btn-secondary", "data-dismiss": "modal" }, "Cancel"),
                                h("button", { type: "submit", class: "btn btn-primary" }, "Save"))))))));
    }
    static get is() { return "wf-activity-editor"; }
    static get originalStyleUrls() { return {
        "$": ["activity-editor.scss"]
    }; }
    static get styleUrls() { return {
        "$": ["activity-editor.css"]
    }; }
    static get properties() { return {
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
        "show": {
            "type": "boolean",
            "mutable": true,
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
            "attribute": "show",
            "reflect": false
        }
    }; }
    static get events() { return [{
            "method": "submit",
            "name": "update-activity",
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
    static get elementRef() { return "el"; }
}
