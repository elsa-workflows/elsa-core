import {Component, h, Prop, State} from '@stencil/core';
import {LiteralExpression, SelectList, SyntaxNames} from "../../models";
import {ActivityInputContext} from "../../services/activity-input-driver";
import {getSelectListItems, getInputPropertyValue} from "../../utils";
import {ExpressionChangedArs} from "../shared/input-control-switch/input-control-switch";

@Component({
  tag: 'elsa-radio-list-input',
  shadow: false
})
export class RadioList {
  private selectList: SelectList = {items: [], isFlagsEnum: false};

  @Prop() public inputContext: ActivityInputContext;
  @State() private selectedValue?: string;

  public async componentWillLoad() {
    this.selectList = await getSelectListItems(this.inputContext.inputDescriptor);
    this.selectedValue = this.getSelectedValue();
  }

  public render() {
    const inputContext = this.inputContext;
    const inputDescriptor = inputContext.inputDescriptor;
    const fieldName = inputDescriptor.name;
    const fieldId = inputDescriptor.name;
    const displayName = inputDescriptor.displayName;
    const hint = inputDescriptor.description;
    const input = getInputPropertyValue(inputContext);
    const value = this.getSelectedValue();
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
            const isSelected = this.selectedValue === value as string;

            return (
              <div class="relative flex items-start">
                <div class="flex items-center h-5">
                  <input id={inputId} type="radio" name={fieldName} checked={isSelected} value={value}
                         onChange={e => this.onCheckChanged(e)}
                         class="focus:ring-blue-500 h-4 w-4 text-blue-600 border-gray-300"/>
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

  private getSelectedValue = (): string => {
    const inputContext = this.inputContext;
    const inputDescriptor = inputContext.inputDescriptor;
    const defaultValue = inputDescriptor.defaultValue;
    const input = getInputPropertyValue(inputContext);
    return ((input?.expression as LiteralExpression)?.value) ?? defaultValue;
  };

  private onCheckChanged = (e: Event) => {
    const checkbox = (e.target as HTMLInputElement);
    const value = checkbox.value;

    this.inputContext.inputChanged(value, SyntaxNames.Literal);
  }

  private onExpressionChanged = (e: CustomEvent<ExpressionChangedArs>) => {
    this.inputContext.inputChanged(e.detail.expression, e.detail.syntax);
  }
}
