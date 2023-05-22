import {Component, h, Prop, Event, EventEmitter, State, Watch} from '@stencil/core';
import {uniq} from 'lodash';

@Component({
  tag: 'elsa-input-tags',
  styleUrl: 'input-tags.css',
  shadow: false,
})
export class InputTags {

  @Prop() fieldId?: string;
  @Prop() placeHolder?: string = 'Add tag';
  @Prop({mutable: true}) values?: Array<string> = [];
  @Event({bubbles: true}) valueChanged: EventEmitter<Array<string>>;

  private addItem = async (item: string) => {
    const values = uniq([...this.values || [], item]);
    this.values = values;
    await this.valueChanged.emit(values);
  };

  private async onInputKeyDown(e: KeyboardEvent) {
    if (e.key != "Enter")
      return;

    e.preventDefault();

    const input = e.target as HTMLInputElement;
    const value = input.value.trim();

    if (value.length == 0)
      return;

    await this.addItem(value);
    input.value = '';
  }

  async onInputBlur(e: Event) {
    const input = e.target as HTMLInputElement;
    const value = input.value.trim();

    if (value.length == 0)
      return;

    await this.addItem(value);
    input.value = '';
  }

  async onDeleteTagClick(e: Event, tag: string) {
    e.preventDefault();

    this.values = this.values.filter(x => x !== tag);
    await this.valueChanged.emit(this.values);
  }

  render() {
    let values = this.values || [];

    if (!Array.isArray(values))
      values = [];

    return (
      <div class="tw-py-2 tw-px-3 tw-bg-white tw-shadow-sm tw-border tw-border-gray-300 tw-rounded-md">
        {values.map(value => (
          <a href="#" onClick={e => this.onDeleteTagClick(e, value)} class="tw-inline-block tw-text-xs tw-bg-blue-400 tw-text-white tw-py-2 tw-px-3 tw-mr-1 tw-mb-1 tw-rounded">
            <span>{value}</span>
            <span class="tw-text-white hover:tw-text-white tw-ml-1">&times;</span>
          </a>
        ))}
        <input type="text" id={this.fieldId}
               onKeyDown={e => this.onInputKeyDown(e)}
               onBlur={e => this.onInputBlur(e)}
               class="tag-input tw-inline-block tw-text-sm tw-outline-none focus:tw-outline-none tw-border-none tw-shadow-none focus:tw-border-none focus:tw-border-transparent focus:tw-shadow-none"
               placeholder={this.placeHolder}/>
      </div>
    )
  }
}
