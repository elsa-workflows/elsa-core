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
        <div class={`${this.active ? '' : 'elsa-hidden'} elsa-overflow-y-auto elsa-h-full`}>
          <slot/>
        </div>
      </Host>
    );
  }

}
