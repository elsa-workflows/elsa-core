import { h, Host } from "@stencil/core";
export class ListField {
    render() {
        const name = this.name;
        const label = this.label;
        const items = this.items;
        return (h(Host, null,
            h("label", { htmlFor: name }, label),
            h("input", { id: name, name: name, type: "text", class: "form-control", value: items }),
            h("small", { class: "form-text text-muted" }, this.hint)));
    }
    static get is() { return "wf-list-field"; }
    static get originalStyleUrls() { return {
        "$": ["list-field.scss"]
    }; }
    static get styleUrls() { return {
        "$": ["list-field.css"]
    }; }
    static get properties() { return {
        "name": {
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
            "attribute": "name",
            "reflect": false
        },
        "label": {
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
            "attribute": "label",
            "reflect": false
        },
        "items": {
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
            "attribute": "items",
            "reflect": false
        },
        "hint": {
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
            "attribute": "hint",
            "reflect": false
        }
    }; }
}
