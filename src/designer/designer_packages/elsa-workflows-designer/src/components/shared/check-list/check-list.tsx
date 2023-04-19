import {Component, h, Prop, Event, EventEmitter} from '@stencil/core';
import {uniq} from 'lodash'
import {SelectList} from "../../../models";

@Component({
  tag: 'elsa-check-list',
  shadow: false
})
export class CheckList {
  @Prop() selectList: SelectList = {items: [], isFlagsEnum: false};
  @Prop({mutable: true}) selectedValues?: Array<string> = [];
  @Prop({mutable: true}) selectedValue?: number;
  @Prop() fieldName: string;
  @Event() selectedValuesChanged: EventEmitter<Array<string> | number>;

  public render() {
    const selectList = this.selectList;
    const fieldName = this.fieldName;

    return (
      <div class="max-w-lg space-y-4 my-4">
        {selectList.items.map((item, index) => {
          const inputId = `${fieldName}_check-list_${index}`;
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
    );
  }

  private getSelectedValues = (selectList: SelectList): number | Array<string> => {
    return selectList.isFlagsEnum ? this.selectedValue : this.selectedValues || [];
  };

  private onCheckChanged = (e: Event) => {
    const checkbox = (e.target as HTMLInputElement);
    const checked = checkbox.checked;
    const value = checkbox.value;
    const isFlags = this.selectList.isFlagsEnum;

    if (isFlags) {
      let newValue = this.selectedValue;

      if (checked)
        newValue = newValue | parseInt(value);
      else
        newValue = newValue & ~parseInt(value);

      this.selectedValue = newValue;
      this.selectedValuesChanged.emit(newValue);
    } else {
      let newValue = this.selectedValues;

      if (checked)
        newValue = uniq([...newValue, value]);
      else
        newValue = newValue.filter(x => x !== value);

      this.selectedValues = newValue;
      this.selectedValuesChanged.emit(newValue);
    }
  }
}
