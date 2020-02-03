import { r as registerInstance, h } from './chunk-25ccd4a5.js';

class ExpressionField {
    constructor(hostRef) {
        registerInstance(this, hostRef);
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
        return (h("host", null, h("label", { htmlFor: name }, label), h("div", { class: "input-group" }, h("input", { name: `${name}.syntax`, value: selectedSyntax, type: "hidden" }), this.renderInputField(), h("div", { class: "input-group-append" }, h("button", { class: "btn btn-primary dropdown-toggle", type: "button", id: `${name}_dropdownMenuButton`, "data-toggle": "dropdown", "aria-haspopup": "true", "aria-expanded": "false" }, selectedSyntax), h("div", { class: "dropdown-menu", "aria-labelledby": `${name}_dropdownMenuButton` }, syntaxes.map(x => h("a", { onClick: e => this.onSyntaxOptionClick(e, x), class: "dropdown-item", href: "#" }, x))))), h("small", { class: "form-text text-muted" }, hint)));
    }
    static get style() { return "wf-expression-field .input-group > .form-control:not(:last-child) {\n  border-top-left-radius: 0.25rem;\n  border-bottom-left-radius: 0.25rem;\n}"; }
}

export { ExpressionField as wf_expression_field };
