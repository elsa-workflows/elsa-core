import {Component, h, Prop, State} from '@stencil/core';
import {LiteralExpression, SelectList, SyntaxNames} from "../../models";
import {NodeInputContext} from "../../services/node-input-driver";
import {getSelectListItems, getInputPropertyValue, setInputPropertyValue} from "../../utils";

@Component({
  tag: 'elsa-radio-list-input',
  shadow: false
})
export class RadioList {
  private selectList: SelectList = {items: [], isFlagsEnum: false};

  @Prop() public inputContext: NodeInputContext;
  @State() private selectedValue?: string;

  public async componentWillLoad() {
    this.selectList = await getSelectListItems(this.inputContext.inputDescriptor);
    this.selectedValue = this.getSelectedValue();
  }

  public render() {
    const inputContext = this.inputContext;
    const inputProperty = inputContext.inputDescriptor;
    const fieldId = inputProperty.name;
    const selectList = this.selectList;

    return (
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
                <input id={inputId} type="radio" name={fieldId} checked={isSelected} value={value}
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
    );
  }

  private getSelectedValue = (): string => {
    const input = getInputPropertyValue(this.inputContext);
    return (input?.expression as LiteralExpression)?.value;
  };

  private onCheckChanged = (e: Event) => {
    const checkbox = (e.target as HTMLInputElement);
    const value = checkbox.value;

    setInputPropertyValue(this.inputContext, value);
    this.inputContext.inputChanged(value, SyntaxNames.Literal);
  }
}
