import { h, Host } from "@stencil/core";
export class BooleanField {
    render() {
        const name = this.name;
        return (h("div", { class: "form-group" },
            h("div", { class: "form-check" },
                h("input", { id: name, name: name, class: "form-check-input", type: "checkbox", value: 'true', checked: this.checked }),
                h("label", { class: "form-check-label", htmlFor: name }, this.label)),
            h("small", { class: "form-text text-muted" }, this.hint)));
    }
    static get is() { return "wf-boolean-field"; }
    static get originalStyleUrls() { return {
        "$": ["boolean-field.scss"]
    }; }
    static get styleUrls() { return {
        "$": ["boolean-field.css"]
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
        "checked": {
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
            "attribute": "checked",
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
