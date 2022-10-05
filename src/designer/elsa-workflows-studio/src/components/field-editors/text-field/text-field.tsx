import { Component, h, Host, Prop } from '@stencil/core';

@Component({
  tag: 'wf-text-field',
  styleUrl: 'text-field.scss',
  shadow: false
})
export class TextField {

  @Prop({reflect: true})
  name: string;

  @Prop({reflect: true})
  label: string;

  @Prop({reflect: true})
  value: string;

  @Prop({reflect: true})
  hint: string;

  render() {
    const name = this.name;

    return (
      <host>
        <label htmlFor={ name }>{ this.label }</label>
        <input id={ name } name={ name } type="text" class="form-control" value={ this.value } />
        <small class="form-text text-muted">{ this.hint }</small>
      </host>);
  }
}
