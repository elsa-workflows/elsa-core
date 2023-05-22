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
        <div class="tw-max-w-lg tw-space-y-4 tw-my-4">
          {selectList.items.map((item, index) => {
            const inputId = `${fieldId}_${index}`;
            const optionIsString = typeof (item as any) == 'string';
            const value = optionIsString ? item : item.value;
            const text = optionIsString ? item : item.text;
            const isSelected = this.selectedValue === value as string;

            return (
              <div class="tw-relative tw-flex tw-items-start">
                <div class="tw-flex tw-items-center tw-h-5">
                  <input id={inputId} type="radio" name={fieldName} checked={isSelected} value={value}
                         onChange={e => this.onCheckChanged(e)}
                         class="focus:tw-ring-blue-500 tw-h-4 tw-w-4 tw-text-blue-600 tw-border-gray-300"/>
                </div>
                <div class="tw-ml-3 tw-text-sm">
                  <label htmlFor={inputId} class="tw-font-medium tw-text-gray-700">{text}</label>
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
