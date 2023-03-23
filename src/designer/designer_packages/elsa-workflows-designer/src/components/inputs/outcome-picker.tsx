import {Component, Prop, h} from '@stencil/core';
import {ObjectExpression, LiteralExpression, SyntaxNames, Variable} from "../../models";
import {ActivityInputContext} from "../../services/activity-input-driver";
import {getInputPropertyValue, getPropertyValue} from "../../utils";
import {FormEntry} from "../shared/forms/form-entry";
import WorkflowDefinitionTunnel from "../../modules/workflow-definitions/state";
import {OutputDefinition} from "../../modules/workflow-definitions/models/entities";
import {ExpressionChangedArs} from "../shared/input-control-switch/input-control-switch";

@Component({
  tag: 'elsa-outcome-picker-input',
  shadow: false
})
export class OutcomePicker {
  @Prop() public inputContext: ActivityInputContext;

  public render() {
    const inputContext = this.inputContext;
    const inputDescriptor = inputContext.inputDescriptor;
    const fieldName = inputDescriptor.name;
    const fieldId = inputDescriptor.name;
    const displayName = inputDescriptor.displayName;
    const description = inputDescriptor.description;
    const input = getInputPropertyValue(inputContext);
    const value = (input?.expression as ObjectExpression)?.value;
    const syntax = input?.expression?.type ?? inputDescriptor.defaultSyntax;

    return (
      <WorkflowDefinitionTunnel.Consumer>
        {({workflowDefinition}) => {
          let outcomes: string[] = workflowDefinition?.outcomes ?? [];
          outcomes = [null, ...outcomes];
          return <elsa-input-control-switch label={displayName} hint={description} syntax={syntax} expression={value} onExpressionChanged={this.onExpressionChanged}>
            <select id={fieldId} name={fieldName} onChange={e => this.onChange(e)}>
              {outcomes.map((outcome: string) => {
                const displayName = outcome ?? '';
                const isSelected = outcome == value;
                return <option value={outcome} selected={isSelected}>{displayName}</option>;
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
    const outcome = inputElement.value;
    this.inputContext.inputChanged(outcome, SyntaxNames.Object);
  }
}
