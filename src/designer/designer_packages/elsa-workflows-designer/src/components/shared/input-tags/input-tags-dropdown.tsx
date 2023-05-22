import {Component, h, Prop, Event, EventEmitter, State, Watch} from '@stencil/core';
import {uniq} from 'lodash';
import {SelectListItem} from '../../../models';

@Component({
  tag: 'elsa-input-tags-dropdown',
  shadow: false,
})
export class InputTagsDropdown {

  @Prop() fieldName?: string;
  @Prop() fieldId?: string;
  @Prop() placeHolder?: string = 'Add tag';
  @Prop() values?: Array<string | SelectListItem> = [];
  @Prop() dropdownValues?: Array<SelectListItem> = [];
  @Event({bubbles: true}) valueChanged: EventEmitter<SelectListItem[]| string[]>;
  @State() currentValues?: Array<SelectListItem> = [];

  @Watch('values')
  private valuesChangedHandler(newValue: Array<string | SelectListItem>) {
    this.updateCurrentValues(newValue);
  }

  public componentWillLoad() {
    this.updateCurrentValues(this.values);
  }

  private updateCurrentValues = (newValue: Array<string | SelectListItem>) => {
    const dropdownValues = this.dropdownValues || [];
    let values: Array<SelectListItem> = [];

    if (!!newValue) {
      newValue.forEach(value => {
        const valueKey = typeof (value) == 'string' ? value as string : (value as SelectListItem).value;
        const tag = dropdownValues.find(x => x.value == valueKey);

        if (!!tag)
          values.push(tag);
      })
    }

    this.currentValues = values;
  };

  private async onTagSelected(e: any) {
    e.preventDefault();

    const input = e.target as HTMLSelectElement;
    const currentTag: SelectListItem = {
      text: input.options[input.selectedIndex].text.trim(),
      value: input.value
    }

    if (currentTag.value.length == 0)
      return;

    const values: Array<SelectListItem> = uniq([...this.currentValues, currentTag]);
    input.value = "Add";
    await this.valueChanged.emit(values);
  }

  async onDeleteTagClick(e: any, currentTag: SelectListItem) {
    e.preventDefault();

    this.currentValues = this.currentValues.filter(tag => tag.value !== currentTag.value);
    await this.valueChanged.emit(this.currentValues);
  }

  render() {
    let values: Array<SelectListItem> = this.currentValues || [];
    let dropdownItems = this.dropdownValues.filter(x => values.findIndex(y => y.value === x.value) < 0);

    if (!Array.isArray(values))
      values = [];

    const valuesJson = JSON.stringify(values.map(tag => tag.value));

    return (
      <div class="tw-py-2 tw-px-3 tw-bg-white tw-shadow-sm tw-border tw-border-gray-300 tw-rounded-md">
        {values.map(tag => (
          <a href="#" onClick={e => this.onDeleteTagClick(e, tag)} class="tw-inline-block tw-text-xs tw-bg-blue-400 tw-text-white tw-py-2 tw-px-3 tw-mr-1 tw-mb-1 tw-rounded">
            <input type="hidden" value={tag.value}/>
            <span>{tag.text}</span>
            <span class="tw-text-white hover:tw-text-white tw-ml-1">&times;</span>
          </a>
        ))}

        <select
          id={this.fieldId}
          class="tw-inline-block tw-text-xs tw-py-2 tw-px-3 tw-mr-1 tw-mb-1 tw-pr-8 tw-border-gray-300 focus:tw-outline-none focus:tw-ring-blue-500 focus:tw-border-blue-500 tw-rounded"
          onChange={(e) => this.onTagSelected(e)}>
          <option value="Add" disabled selected>{this.placeHolder}</option>
          {dropdownItems.map(tag => <option value={tag.value}>{tag.text}</option>)}
        </select>
        <input type="hidden" name={this.fieldName} value={valuesJson}/>
      </div>
    )
  }
}
