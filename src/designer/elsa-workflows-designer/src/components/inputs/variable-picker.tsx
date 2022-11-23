import {Component, Prop, h} from '@stencil/core';
import {LiteralExpression, SelectList, SyntaxNames, Variable} from "../../models";
import {ActivityInputContext} from "../../services/node-input-driver";
import {getInputPropertyValue, getPropertyValue, getSelectListItems} from "../../utils";
import {ExpressionChangedArs} from "../designer/input-control-switch/input-control-switch";
import WorkflowDefinitionEditorTunnel from "../../modules/workflow-definitions/state";
import {FormEntry} from "../shared/forms/form-entry";

@Component({
  tag: 'elsa-variable-picker-input',
  shadow: false
})
export class VariablePickerInput {
  @Prop() public inputContext: ActivityInputContext;

  public render() {
    const inputContext = this.inputContext;
    const inputDescriptor = inputContext.inputDescriptor;
    const fieldName = inputDescriptor.name;
    const fieldId = inputDescriptor.name;
    const displayName = inputDescriptor.displayName;
    const description = inputDescriptor.description;
    const value = getPropertyValue(inputContext) as Variable;
    let currentValue = value;

    if (currentValue == undefined) {
      const defaultValue = inputDescriptor.defaultValue;
      currentValue = defaultValue ? defaultValue.toString() : undefined;
    }

    debugger;

    return (
      <WorkflowDefinitionEditorTunnel.Consumer>
        {({workflowDefinition}) => {
          const variables = workflowDefinition?.variables ?? [];
          return<FormEntry fieldId={fieldId} label={displayName} hint={description}>
          <select id={fieldId} name={fieldName} onChange={e => this.onChange(e)}>
              {variables.map((variable: Variable) => {
                const isSelected = variable.name == currentValue?.name;
                const json = JSON.stringify(variable);
                return <option value={variable.name} selected={isSelected} data-variable={json}>{variable.name}</option>;
              })}
            </select>
          </FormEntry>
        }}
      </WorkflowDefinitionEditorTunnel.Consumer>
    );
  }

  private onChange = (e: Event) => {
    debugger;
    const inputElement = e.target as HTMLSelectElement;
    const variable = inputElement.selectedOptions.length == 0 ? null : JSON.parse(inputElement.selectedOptions[0].dataset.variable);
    this.inputContext.inputChanged(variable, SyntaxNames.Literal);
  }
}
