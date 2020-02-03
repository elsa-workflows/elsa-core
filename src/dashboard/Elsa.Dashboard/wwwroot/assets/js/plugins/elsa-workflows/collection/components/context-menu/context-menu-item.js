import { h } from "@stencil/core";
export class ContextMenuItem {
    render() {
        const text = this.text;
        return (h("a", { class: "dropdown-item", href: "#", onClick: e => e.preventDefault() }, text));
    }
    static get is() { return "wf-context-menu-item"; }
    static get originalStyleUrls() { return {
        "$": ["context-menu-item.scss"]
    }; }
    static get styleUrls() { return {
        "$": ["context-menu-item.css"]
    }; }
    static get properties() { return {
        "text": {
            "type": "any",
            "mutable": false,
            "complexType": {
                "original": "any",
                "resolved": "any",
                "references": {}
            },
            "required": false,
            "optional": false,
            "docs": {
                "tags": [],
                "text": ""
            },
            "attribute": "text",
            "reflect": true
        }
    }; }
    static get elementRef() { return "el"; }
}
