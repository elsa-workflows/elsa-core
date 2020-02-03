export class ExpressionFieldDriver {
    constructor() {
        this.displayEditor = (activity, property) => {
            const name = property.name;
            const label = property.label;
            const value = activity.state[name] || { expression: '', syntax: 'Literal' };
            const multiline = (property.options || {}).multiline || false;
            const expressionValue = value.expression.replace(/"/g, '&quot;');
            return `<wf-expression-field name="${name}" label="${label}" hint="${property.hint}" value="${expressionValue}" syntax="${value.syntax}" multiline="${multiline}"></wf-expression-field>`;
        };
        this.updateEditor = (activity, property, formData) => {
            const expressionFieldName = `${property.name}.expression`;
            const syntaxFieldName = `${property.name}.syntax`;
            const expression = formData.get(expressionFieldName).toString().trim();
            const syntax = formData.get(syntaxFieldName).toString();
            activity.state[property.name] = {
                expression: expression,
                syntax: syntax
            };
        };
    }
}
