import {FieldDriver} from "../servicesCopy/field-driver";
import {Activity, ActivityPropertyDescriptor, RenderResult, WorkflowExpression} from "../modelsCopy";

export class ExpressionFieldDriver implements FieldDriver {
  displayEditor = (activity: Activity, property: ActivityPropertyDescriptor): RenderResult => {
    const name = property.name;
    const label = property.label;
    const value: WorkflowExpression = activity.state[name] || { expression: '', syntax: 'Literal' };
    const multiline: boolean = (property.options || {}).multiline || false;
    const expressionValue = value.expression.replace(/"/g, '&quot;');

    return `<wf-expression-field name="${name}" label="${label}" hint="${property.hint}" value="${expressionValue}" syntax="${value.syntax}" multiline="${multiline}"></wf-expression-field>`;
  };

  updateEditor = (activity: Activity, property: ActivityPropertyDescriptor, formData: FormData) => {
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
