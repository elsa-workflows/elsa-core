import {Component, Prop, h} from '@stencil/core';
import {SyntaxNames, Variable} from "../../models";
import {ActivityInputContext} from "../../services/activity-input-driver";
import {getPropertyValue} from "../../utils";
import {FormEntry} from "../shared/forms/form-entry";
import WorkflowDefinitionTunnel from "../../modules/workflow-definitions/state";

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

    return (
      <WorkflowDefinitionTunnel.Consumer>
        {({workflowDefinition}) => {
          let  variables: Variable[] = workflowDefinition?.variables ?? [];
          variables = [null, ...variables];
          return <FormEntry fieldId={fieldId} label={displayName} hint={description}>
          <select id={fieldId} name={fieldName} onChange={e => this.onChange(e)}>
              {variables.map((variable: Variable) => {
                const variableName = variable?.name;
                const isSelected = variableName == currentValue?.name;
                const json = variable ? JSON.stringify(variable) : '';
                return <option value={variableName} selected={isSelected} data-variable={json}>{variableName}</option>;
              })}
            </select>
          </FormEntry>
        }}
      </WorkflowDefinitionTunnel.Consumer>
    );
  }

  private onChange = (e: Event) => {
    const inputElement = e.target as HTMLSelectElement;
    const json = inputElement.selectedOptions[0].dataset.variable;
    const variable = inputElement.selectedIndex <= 0 ? null : JSON.parse(json);
    this.inputContext.inputChanged(variable, SyntaxNames.Literal);
  }
}
