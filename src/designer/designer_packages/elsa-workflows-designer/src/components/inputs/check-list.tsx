import {Component, h, Prop, State} from '@stencil/core';
import {uniq} from 'lodash'
import {ObjectExpression, SelectList, SyntaxNames} from "../../models";
import {ActivityInputContext} from "../../services/activity-input-driver";
import {getSelectListItems, getInputPropertyValue, parseJson, getObjectOrParseJson} from "../../utils";
import {ExpressionChangedArs} from "../shared/input-control-switch/input-control-switch";

@Component({
  tag: 'elsa-check-list-input',
  shadow: false
})
export class CheckList {
  private selectList: SelectList = {items: [], isFlagsEnum: false};

  @Prop() public inputContext: ActivityInputContext;
  @State() private selectedValues?: Array<string> = [];
  @State() private selectedValue?: number;

  public async componentWillLoad() {
    this.selectList = await getSelectListItems(this.inputContext.inputDescriptor);
    const selectedValues = this.getSelectedValues(this.selectList);

    if (Array.isArray(selectedValues))
      this.selectedValues = selectedValues;
    else if (typeof (selectedValues) == 'number')
      this.selectedValue = selectedValues;
    else if (typeof selectedValues == 'string')
      this.selectedValues = JSON.parse(selectedValues);
  }

  public render() {
    const inputContext = this.inputContext;
    const inputDescriptor = inputContext.inputDescriptor;
    const fieldName = inputDescriptor.name;
    const fieldId = inputDescriptor.name;
    const displayName = inputDescriptor.displayName;
    const hint = inputDescriptor.description;
    const input = getInputPropertyValue(inputContext);
    const value = (input?.expression as ObjectExpression)?.value; // TODO: The "value" field is currently hardcoded, but we should be able to be more flexible and potentially have different fields for a given syntax.
    const syntax = input?.expression?.type ?? inputDescriptor.defaultSyntax;
    const selectList = this.selectList;

    return (
      <elsa-input-control-switch label={displayName} hint={hint} syntax={syntax} expression={value} onExpressionChanged={this.onExpressionChanged}>
        <div class="max-w-lg space-y-4 my-4">
          {selectList.items.map((item, index) => {
            const inputId = `${fieldId}_${index}`;
            const optionIsString = typeof (item as any) == 'string';
            const value = optionIsString ? item : item.value;
            const text = optionIsString ? item : item.text;
            const isSelected = selectList.isFlagsEnum
              ? (this.selectedValue & (parseInt(value as string))) == parseInt(value as string)
              : this.selectedValues.findIndex(x => x == value) >= 0;

            return (
              <div class="relative flex items-start">
                <div class="flex items-center h-5">
                  <input id={inputId} type="checkbox" name={fieldName} checked={isSelected} value={value}
                         onChange={e => this.onCheckChanged(e)}
                         class="focus:ring-blue-500 h-4 w-4 text-blue-600 border-gray-300 rounded"/>
                </div>
                <div class="ml-3 text-sm">
                  <label htmlFor={inputId} class="font-medium text-gray-700">{text}</label>
                </div>
              </div>
            );
          })}
        </div>
      </elsa-input-control-switch>
    );
  }

  private getSelectedValues = (selectList: SelectList): number | Array<string> => {
    const inputContext = this.inputContext;
    const inputDescriptor = inputContext.inputDescriptor;
    const input = getInputPropertyValue(this.inputContext);
    const defaultValue = inputDescriptor.defaultValue;
    const json = this.getValueOrDefault(input?.expression?.value, defaultValue);
    let parsedValue = selectList.isFlagsEnum ? parseInt(json) : getObjectOrParseJson(json) || [];

    if (parsedValue.length == 0)
      parsedValue = getObjectOrParseJson(defaultValue) || [];

    return parsedValue;
  };

  private getValueOrDefault(value: string | undefined, defaultValue: string | undefined) {
    return value ?? defaultValue ?? '';
  }

  private onCheckChanged = (e: Event) => {
    const checkbox = (e.target as HTMLInputElement);
    const checked = checkbox.checked;
    const value = checkbox.value;
    const isFlags = this.selectList.isFlagsEnum;
    let json = '[]';

    if (isFlags) {
      let newValue = this.selectedValue;

      if (checked)
        newValue = newValue | parseInt(value);
      else
        newValue = newValue & ~parseInt(value);

      this.selectedValue = newValue;
      json = newValue.toString();
    } else {
      let newValue = this.selectedValues;

      if (checked)
        newValue = uniq([...newValue, value]);
      else
        newValue = newValue.filter(x => x !== value);

      this.selectedValues = newValue;
      json = JSON.stringify(newValue);
    }

    this.inputContext.inputChanged(json, SyntaxNames.Object);
  }

  private onExpressionChanged = (e: CustomEvent<ExpressionChangedArs>) => {
    this.inputContext.inputChanged(e.detail.expression, e.detail.syntax);
  }
}
