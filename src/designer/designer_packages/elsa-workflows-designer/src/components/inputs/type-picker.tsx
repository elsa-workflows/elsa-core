import {Component, Prop, h} from '@stencil/core';
import {groupBy} from 'lodash';
import {LiteralExpression, SyntaxNames} from "../../models";
import {ActivityInputContext} from "../../services/activity-input-driver";
import {getInputPropertyValue} from "../../utils";
import descriptorsStore from '../../data/descriptors-store';
import {VariableDescriptor} from "../../services/api-client/variable-descriptors-api";
import {ExpressionChangedArs} from "../shared/input-control-switch/input-control-switch";

@Component({
  tag: 'elsa-type-picker-input',
  shadow: false
})
export class TypePickerInput {
  @Prop() public inputContext: ActivityInputContext;

  public render() {
    const inputContext = this.inputContext;
    const inputDescriptor = inputContext.inputDescriptor;
    const fieldName = inputDescriptor.name;
    const fieldId = inputDescriptor.name;
    const displayName = inputDescriptor.displayName;
    const description = inputDescriptor.description;
    const availableTypes: Array<VariableDescriptor> = descriptorsStore.variableDescriptors;
    const groupedVariableTypes = groupBy(availableTypes, x => x.category);
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
          {Object.keys(groupedVariableTypes).map(category => {
            const variableTypes = groupedVariableTypes[category] as Array<VariableDescriptor>;
            return (<optgroup label={category}>
              {variableTypes.map(descriptor => <option value={descriptor.typeName} selected={descriptor.typeName == currentValue}>{descriptor.displayName}</option>)}
            </optgroup>);
          })}
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
