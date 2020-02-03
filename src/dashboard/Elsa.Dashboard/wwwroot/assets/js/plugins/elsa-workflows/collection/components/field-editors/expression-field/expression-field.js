import { h, Host } from "@stencil/core";
export class ExpressionField {
    constructor() {
        this.selectSyntax = (syntax) => this.syntax = syntax;
        this.onSyntaxOptionClick = (e, syntax) => {
            e.preventDefault();
            this.selectSyntax(syntax);
        };
        this.renderInputField = () => {
            const name = this.name;
            const value = this.value;
            if (this.multiline)
                return h("textarea", { id: name, name: `${name}.expression`, class: "form-control", rows: 3 }, value);
            return h("input", { id: name, name: `${name}.expression`, value: value, type: "text", class: "form-control" });
        };
    }
    render() {
        const name = this.name;
        const label = this.label;
        const hint = this.hint;
        const syntaxes = ['Literal', 'JavaScript', 'Liquid'];
        const selectedSyntax = this.syntax || 'Literal';
        return (h("host", null,
            h("label", { htmlFor: name }, label),
            h("div", { class: "input-group" },
                h("input", { name: `${name}.syntax`, value: selectedSyntax, type: "hidden" }),
                this.renderInputField(),
                h("div", { class: "input-group-append" },
                    h("button", { class: "btn btn-primary dropdown-toggle", type: "button", id: `${name}_dropdownMenuButton`, "data-toggle": "dropdown", "aria-haspopup": "true", "aria-expanded": "false" }, selectedSyntax),
                    h("div", { class: "dropdown-menu", "aria-labelledby": `${name}_dropdownMenuButton` }, syntaxes.map(x => h("a", { onClick: e => this.onSyntaxOptionClick(e, x), class: "dropdown-item", href: "#" }, x))))),
            h("small", { class: "form-text text-muted" }, hint)));
    }
    static get is() { return "wf-expression-field"; }
    static get originalStyleUrls() { return {
        "$": ["expression-field.scss"]
    }; }
    static get styleUrls() { return {
        "$": ["expression-field.css"]
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
        "multiline": {
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
            "attribute": "multiline",
            "reflect": true
        },
        "syntax": {
            "type": "string",
            "mutable": true,
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
            "attribute": "syntax",
            "reflect": true
        }
    }; }
}
