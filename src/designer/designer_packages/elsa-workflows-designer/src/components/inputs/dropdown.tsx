import {Component, Prop, h} from '@stencil/core';
import {LiteralExpression, SelectList, SyntaxNames} from "../../models";
import {ActivityInputContext} from "../../services/activity-input-driver";
import {getInputPropertyValue, getSelectListItems} from "../../utils";
import {ExpressionChangedArs} from "../shared/input-control-switch/input-control-switch";

@Component({
  tag: 'elsa-dropdown-input',
  shadow: false
})
export class DropdownInput {
  @Prop() public inputContext: ActivityInputContext;

  private selectList: SelectList = {items: [], isFlagsEnum: false};

  public async componentWillLoad() {
    this.selectList = await getSelectListItems(this.inputContext.inputDescriptor);
  }

  public render() {
    const inputContext = this.inputContext;
    const inputDescriptor = inputContext.inputDescriptor;
    const fieldName = inputDescriptor.name;
    const fieldId = inputDescriptor.name;
    const displayName = inputDescriptor.displayName;
    const hint = inputDescriptor.description;
    const input = getInputPropertyValue(inputContext);
    const syntax = input?.expression?.type ?? inputDescriptor.defaultSyntax;
    const defaultValue = inputDescriptor.defaultValue;
    const value = this.getValueOrDefault((input?.expression as LiteralExpression)?.value, defaultValue);
    const {items} = this.selectList;
    let currentValue = value;

    if (currentValue == undefined) {
      const defaultValue = inputDescriptor.defaultValue;
      currentValue = defaultValue ? defaultValue.toString() : undefined;
    }

    return (
      <elsa-input-control-switch label={displayName} hint={hint} syntax={syntax} expression={value} onExpressionChanged={this.onExpressionChanged}>
        <select id={fieldId} name={fieldName} onChange={e => this.onChange(e)}>
          {items.map(item => {
            const optionIsObject = typeof (item) == 'object';
            const value = optionIsObject ? item.value : item.toString();
            const text = optionIsObject ? item.text : item.toString();
            return <option value={value} selected={value === currentValue}>{text}</option>;
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

  private getValueOrDefault(value: string | undefined, defaultValue: string | undefined) {
    return value ?? defaultValue ?? '';
  }
}
