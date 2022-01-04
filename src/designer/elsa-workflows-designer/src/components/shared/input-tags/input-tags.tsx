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

    // const valuesJson = JSON.stringify(values);

    return (
      <div class="py-2 px-3 bg-white shadow-sm border border-gray-300 rounded-md">
        {values.map(value => (
          <a href="#" onClick={e => this.onDeleteTagClick(e, value)} class="inline-block text-xs bg-blue-400 text-white py-2 px-3 mr-1 mb-1 rounded">
            <span>{value}</span>
            <span class="text-white hover:text-white ml-1">&times;</span>
          </a>
        ))}
        <input type="text" id={this.fieldId}
               onKeyDown={e => this.onInputKeyDown(e)}
               onBlur={e => this.onInputBlur(e)}
               class="tag-input inline-block text-sm outline-none focus:outline-none border-none shadow:none focus:border-none focus:border-transparent focus:shadow-none"
               placeholder={this.placeHolder}/>
        {/*<input type="hidden" name={this.fieldName} value={valuesJson}/>*/}
      </div>
    )
  }
}
