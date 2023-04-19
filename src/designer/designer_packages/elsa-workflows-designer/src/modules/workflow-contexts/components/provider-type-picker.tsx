import {Component, Prop, h, State} from '@stencil/core';
import {LiteralExpression, SyntaxNames} from "../../../models";
import {ActivityInputContext} from "../../../services/activity-input-driver";
import {getInputPropertyValue} from "../../../utils";
import {ExpressionChangedArs} from "../../../components/shared/input-control-switch/input-control-switch";
import {WorkflowContextProviderDescriptor} from "../services/api";
import WorkflowDefinitionTunnel, {WorkflowDefinitionState} from "../../workflow-definitions/state";
import {WorkflowDefinition} from "../../workflow-definitions/models/entities";
import {WorkflowContextProviderTypesKey} from "../constants";

@Component({
  tag: 'elsa-workflow-context-provider-type-picker-input',
  shadow: false
})
export class ProviderTypePicker {
  @Prop() public inputContext: ActivityInputContext;
  @Prop() descriptors: Array<WorkflowContextProviderDescriptor> = [];
  @Prop() workflowDefinition: WorkflowDefinition;

  public render() {
    const inputContext = this.inputContext;
    const inputDescriptor = inputContext.inputDescriptor;
    const fieldName = inputDescriptor.name;
    const fieldId = inputDescriptor.name;
    const displayName = inputDescriptor.displayName;
    const description = inputDescriptor.description;
    const allProviders: Array<WorkflowContextProviderDescriptor> = this.descriptors;
    const activatedProviders: Array<string> = this.workflowDefinition?.customProperties[WorkflowContextProviderTypesKey] ?? [];
    const availableProviders = allProviders.filter(x => activatedProviders.includes(x.type));
    const input = getInputPropertyValue(inputContext);
    const syntax = input?.expression?.type ?? inputDescriptor.defaultSyntax;
    const value = (input?.expression as LiteralExpression)?.value;
    let currentValue = value;

    if (currentValue == undefined) {
      const defaultValue = inputDescriptor.defaultValue;
      currentValue = defaultValue ? defaultValue.toString() : undefined;
    }

    return (
      <elsa-input-control-switch label={displayName} hint={description} syntax={syntax} expression={value} onExpressionChanged={this.onExpressionChanged}>
        <select id={fieldId} name={fieldName} onChange={e => this.onChange(e)}>
          <option value="" selected={(!currentValue || currentValue == "")}></option>
          {availableProviders.map(descriptor => <option value={descriptor.type} selected={descriptor.type == currentValue}>{descriptor.name}</option>)}
        </select>
      </elsa-input-control-switch>
    );
  }

  private onChange = (e: Event) => {
    const inputElement = e.target as HTMLSelectElement;
    this.inputContext.inputChanged(inputElement.value, SyntaxNames.Literal);
  }

  private onExpressionChanged = (e: CustomEvent<ExpressionChangedArs>) => {
    this.inputContext.inputChanged(e.detail.expression, e.detail.syntax);
  }
}
WorkflowDefinitionTunnel.injectProps(ProviderTypePicker, ['workflowDefinition']);
