import {Component, Prop, h} from '@stencil/core';
import {LiteralExpression, SyntaxNames, Variable} from "../../models";
import {ActivityInputContext} from "../../services/activity-input-driver";
import {getInputPropertyValue, getPropertyValue} from "../../utils";
import {FormEntry} from "../shared/forms/form-entry";
import WorkflowDefinitionTunnel from "../../modules/workflow-definitions/state";
import {OutputDefinition} from "../../modules/workflow-definitions/models/entities";
import {ExpressionChangedArs} from "../shared/input-control-switch/input-control-switch";

@Component({
  tag: 'elsa-output-picker-input',
  shadow: false
})
export class OutputPicker {
  @Prop() public inputContext: ActivityInputContext;

  public render() {
    const inputContext = this.inputContext;
    const inputDescriptor = inputContext.inputDescriptor;
    const fieldName = inputDescriptor.name;
    const fieldId = inputDescriptor.name;
    const displayName = inputDescriptor.displayName;
    const description = inputDescriptor.description;
    const input = getInputPropertyValue(inputContext);
    const value = (input?.expression as LiteralExpression)?.value;
    const syntax = input?.expression?.type ?? inputDescriptor.defaultSyntax;

    return (
      <WorkflowDefinitionTunnel.Consumer>
        {({workflowDefinition}) => {
          let outputs: OutputDefinition[] = workflowDefinition?.outputs ?? [];
          outputs = [null, ...outputs];
          return <elsa-input-control-switch label={displayName} hint={description} syntax={syntax} expression={value} onExpressionChanged={this.onExpressionChanged}>
            <select id={fieldId} name={fieldName} onChange={e => this.onChange(e)}>
              {outputs.map((output: OutputDefinition) => {
                const outputName = output?.name;
                const displayName = output?.displayName ?? '';
                const isSelected = outputName == value;
                return <option value={outputName} selected={isSelected}>{displayName}</option>;
              })}
            </select>
          </elsa-input-control-switch>
        }}
      </WorkflowDefinitionTunnel.Consumer>
    );
  }

  private onExpressionChanged = (e: CustomEvent<ExpressionChangedArs>) => {
    this.inputContext.inputChanged(e.detail.expression, e.detail.syntax);
  }

  private onChange = (e: Event) => {
    const inputElement = e.target as HTMLSelectElement;
    const outputName = inputElement.value;
    this.inputContext.inputChanged(outputName, SyntaxNames.Literal);
  }
}
