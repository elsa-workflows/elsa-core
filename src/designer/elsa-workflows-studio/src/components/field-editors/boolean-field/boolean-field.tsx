import {Component, h, Host, Prop} from '@stencil/core';

@Component({
  tag: 'wf-boolean-field',
  styleUrl: 'boolean-field.scss',
  shadow: false
})
export class BooleanField {

  @Prop({ reflect: true })
  name: string;

  @Prop({ reflect: true })
  label: string;

  @Prop({ reflect: true })
  checked: boolean;

  @Prop({ reflect: true })
  hint: string;

  render() {
    const name = this.name;

    return (
      <div class="form-group">
        <div class="form-check">
          <input id={name} name={name} class="form-check-input" type="checkbox" value={'true'} checked={this.checked}/>
          <label class="form-check-label" htmlFor={name}>{this.label}</label>
        </div>
        <small class="form-text text-muted">{this.hint}</small>
      </div>);
  }
}
