import { Component, h, Host, Prop, Element } from '@stencil/core';
import { SelectGroup, SelectItem, SelectOption, SelectOptionPair } from "./models";

@Component({
  tag: 'wf-select-field',
  styleUrl: 'select-field.scss',
  shadow: false
})
export class SelectField {

  @Element()
  element: HTMLWfSelectFieldElement;

  @Prop()
  name: string;

  @Prop()
  label: string;

  @Prop()
  value: string;

  @Prop()
  hint: string;

  @Prop({ mutable: true })
  items: Array<SelectItem>;

  componentWillLoad() {
    const encodedJson = this.element.getAttribute('data-items');

    if (!encodedJson)
      return;

    const json = decodeURI(encodedJson);
    this.items = JSON.parse(json);
  }

  render() {
    const name = this.name;
    const label = this.label;
    const items: Array<SelectItem> = this.items || [];

    return (
      <Host>
        <label htmlFor={ name }>{ label }</label>
        <select id={ name } name={ name } class="custom-select">
          { items.map(this.renderItem) }
        </select>
        <small class="form-text text-muted">{ this.hint }</small>
      </Host>);
  }

  renderItem = (item: SelectItem) => {
    const isGroup = !!(item as SelectGroup).options;

    return isGroup ? this.renderGroup(item as SelectGroup) : this.renderOption(item as SelectOption);
  };

  renderOption = (option: SelectOption) => {
    const type = typeof (option);
    let label = null;
    let value = null;

    switch (type) {
      case 'string':
        label = option;
        value = option;
        break;
      case 'number':
        label = option.toString();
        value = option.toString();
        break;
      case 'object':
        const pair = option as SelectOptionPair;
        label = pair.label;
        value = pair.value;
        break;
      default:
        throw Error(`Unsupported option type: ${type}.`);
    }

    const isSelected = value === this.value;
    return <option value={ value } selected={ isSelected }>{ label }</option>;
  };

  renderGroup = (group: SelectGroup) => {
    return (
      <optgroup label={ group.label }>
        { group.options.map(this.renderOption) }
      </optgroup>
    );
  };
}
