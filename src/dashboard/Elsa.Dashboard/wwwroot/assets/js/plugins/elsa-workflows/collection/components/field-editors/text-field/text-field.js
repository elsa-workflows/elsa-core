import { h, Host } from "@stencil/core";
export class TextField {
    render() {
        const name = this.name;
        return (h("host", null,
            h("label", { htmlFor: name }, this.label),
            h("input", { id: name, name: name, type: "text", class: "form-control", value: this.value }),
            h("small", { class: "form-text text-muted" }, this.hint)));
    }
    static get is() { return "wf-text-field"; }
    static get originalStyleUrls() { return {
        "$": ["text-field.scss"]
    }; }
    static get styleUrls() { return {
        "$": ["text-field.css"]
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
            "reflect": true
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
            "reflect": true
        },
        "value": {
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
            "attribute": "value",
            "reflect": true
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
            "reflect": true
        }
    }; }
}
