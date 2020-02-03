import { h } from "@stencil/core";
export class ActivityPicker {
    constructor() {
        this.activityDefinitions = [];
        this.filterText = '';
        this.selectedCategory = null;
        this.onFilterTextChanged = (e) => {
            const filterField = e.target;
            this.filterText = filterField.value;
        };
        this.selectCategory = (category) => {
            this.selectedCategory = category;
        };
        this.renderActivity = (activity) => {
            const icon = activity.icon || 'fas fa-cog';
            const iconClass = `${icon} mr-1`;
            return (h("div", { class: "card activity" },
                h("div", { class: "card-body" },
                    h("h4", { class: "card-title" },
                        h("i", { class: iconClass }),
                        activity.displayName),
                    h("p", null, activity.description),
                    h("a", { href: "#", onClick: e => {
                            e.preventDefault();
                            this.selectCategory(activity.category);
                        } },
                        h("span", { class: "badge badge-light" }, activity.category))),
                h("div", { class: "card-footer text-muted text-xs-right" },
                    h("a", { class: "btn btn-primary btn-sm", href: "#", onClick: async (e) => {
                            e.preventDefault();
                            await this.onActivitySelected(activity);
                        } }, "Select"))));
        };
    }
    async show() {
        this.isVisible = true;
        $(this.modal).modal('show');
    }
    async hide() {
        $(this.modal).modal('hide');
        this.isVisible = false;
    }
    async onActivitySelected(activity) {
        this.activitySelected.emit(activity);
        await this.hide();
    }
    render() {
        const activities = this.activityDefinitions;
        const categories = [null, ...new Set(activities.map(x => x.category))];
        const filterText = this.filterText;
        const selectedCategory = this.selectedCategory;
        let definitions = activities;
        if (!!selectedCategory)
            definitions = definitions.filter(x => x.category.toLowerCase() === selectedCategory.toLowerCase());
        if (!!filterText)
            definitions = definitions.filter(x => x.displayName.toLowerCase().includes(filterText.toLowerCase()));
        return (h("div", null,
            h("div", { class: "modal", tabindex: "-1", role: "dialog", ref: el => this.modal = el },
                h("div", { class: "modal-dialog modal-xl", role: "document" },
                    h("div", { class: "modal-content" },
                        h("div", { class: "modal-header" },
                            h("h5", { class: "modal-title" }, "Available Activities"),
                            h("button", { type: "button", class: "close", "data-dismiss": "modal", "aria-label": "Close" },
                                h("span", { "aria-hidden": "true" }, "\u00D7"))),
                        h("div", { class: "modal-body" },
                            h("div", { class: "row" },
                                h("div", { class: "col-sm-3 col-md-3 col-lg-2" },
                                    h("div", { class: "form-group" },
                                        h("input", { class: "form-control", type: "search", placeholder: "Filter", "aria-label": "Filter", autofocus: true, onKeyUp: this.onFilterTextChanged })),
                                    h("ul", { class: "nav nav-pills flex-column activity-picker-categories" }, categories.map(category => {
                                        const categoryDisplayText = category || 'All';
                                        const isSelected = category === this.selectedCategory;
                                        const classes = { 'nav-link': true, 'active': isSelected };
                                        return (h("li", { class: "nav-item" },
                                            h("a", { class: classes, href: "#", "data-toggle": "pill", onClick: e => {
                                                    e.preventDefault();
                                                    this.selectCategory(category);
                                                } }, categoryDisplayText)));
                                    }))),
                                h("div", { class: "col-sm-9 col-md-9 col-lg-10" },
                                    h("div", { class: "card-columns tab-content" }, definitions.map(this.renderActivity))))),
                        h("div", { class: "modal-footer" },
                            h("button", { type: "button", class: "btn btn-secondary", "data-dismiss": "modal" }, "Cancel")))))));
    }
    static get is() { return "wf-activity-picker"; }
    static get originalStyleUrls() { return {
        "$": ["activity-picker.scss"]
    }; }
    static get styleUrls() { return {
        "$": ["activity-picker.css"]
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
        }
    }; }
    static get states() { return {
        "isVisible": {},
        "filterText": {},
        "selectedCategory": {}
    }; }
    static get events() { return [{
            "method": "activitySelected",
            "name": "activity-picked",
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
        "show": {
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
        "hide": {
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
}
