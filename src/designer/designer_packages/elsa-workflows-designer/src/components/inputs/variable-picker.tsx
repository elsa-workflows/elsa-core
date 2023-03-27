import {Component, h, Prop} from '@stencil/core';
import {SyntaxNames, Variable} from "../../models";
import {ActivityInputContext} from "../../services/activity-input-driver";
import {getPropertyValue} from "../../utils";
import {FormEntry} from "../shared/forms/form-entry";
import WorkflowDefinitionTunnel from "../../modules/workflow-definitions/state";
import {WorkflowDefinition} from "../../modules/workflow-definitions/models/entities";

@Component({
  tag: 'elsa-variable-picker-input',
  shadow: false
})
export class VariablePickerInput {
  @Prop() inputContext: ActivityInputContext;
  @Prop() workflowDefinition: WorkflowDefinition; // Injected by WorkflowDefinitionTunnel

  public render() {
    const inputContext = this.inputContext;
    const inputDescriptor = inputContext.inputDescriptor;
    const fieldName = inputDescriptor.name;
    const fieldId = inputDescriptor.name;
    const displayName = inputDescriptor.displayName;
    const description = inputDescriptor.description;
    let currentValue = getPropertyValue(inputContext) as Variable;

    if (currentValue == undefined) {
      const defaultValue = inputDescriptor.defaultValue;
      currentValue = defaultValue ? defaultValue.toString() : undefined;
    }

    return (
      <WorkflowDefinitionTunnel.Consumer>
        {({workflowDefinition}) => {
          let variables: Variable[] = workflowDefinition?.variables ?? [];
          variables = [null, ...variables];
          return <FormEntry fieldId={fieldId} label={displayName} hint={description}>
            <select id={fieldId} name={fieldName} onChange={e => this.onChange(e)}>
              {variables.map((variable: Variable) => {
                const variableName = variable?.name;
                const variableId = variable?.id;
                const isSelected = variableId == currentValue?.id;
                return <option value={variableId} selected={isSelected}>{variableName}</option>;
              })}
            </select>
          </FormEntry>
        }}
      </WorkflowDefinitionTunnel.Consumer>
    );
  }

  private onChange = (e: Event) => {
    const inputElement = e.target as HTMLSelectElement;
    const variableId = inputElement.value;
    const variable = this.workflowDefinition.variables.find(x => x.id == variableId);

    this.inputContext.inputChanged(variable, SyntaxNames.Literal);
  }
}
WorkflowDefinitionTunnel.injectProps(VariablePickerInput, ['workflowDefinition']);
