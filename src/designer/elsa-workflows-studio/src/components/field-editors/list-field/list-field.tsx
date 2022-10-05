import { Component, h, Host, Prop } from '@stencil/core';

@Component({
  tag: 'wf-list-field',
  styleUrl: 'list-field.scss',
  shadow: false
})
export class ListField {

  @Prop()
  name: string;

  @Prop()
  label: string;

  @Prop()
  items: string;

  @Prop()
  hint: string;

  render() {
    const name = this.name;
    const label = this.label;
    const items = this.items;

    return (
      <Host>
        <label htmlFor={ name }>{ label }</label>
        <input id={ name } name={ name } type="text" class="form-control" value={ items } />
        <small class="form-text text-muted">{ this.hint }</small>
      </Host>);
  }
}
