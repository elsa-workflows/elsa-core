import {Component, Host, h, Prop} from '@stencil/core';

@Component({
  tag: 'elsa-tab-content',
  shadow: false,
})
export class ElsaTabContent {
  @Prop() tab: string;
  @Prop() active: boolean;

  render() {
    return (
      <Host>
        <div class={this.active ? '' : 'elsa-hidden'}>
          <slot/>
        </div>
      </Host>
    );
  }

}
