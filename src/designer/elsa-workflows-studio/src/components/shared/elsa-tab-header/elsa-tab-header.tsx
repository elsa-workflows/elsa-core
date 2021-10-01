import {Component, Host, h, Prop} from '@stencil/core';

@Component({
  tag: 'elsa-tab-header',
  shadow: false,
})

export class ElsaTabHeader {
  @Prop() tab: string;
  @Prop() active: boolean;

  render() {
    const className = this.active ? 'elsa-border-blue-500 elsa-text-blue-600' : 'elsa-border-transparent elsa-text-gray-500 hover:elsa-text-gray-700 hover:elsa-border-gray-300';

    return (
      <Host>
        <div class={`${className} elsa-cursor-pointer elsa-whitespace-nowrap elsa-py-4 elsa-px-1 elsa-border-b-2 elsa-font-medium elsa-text-sm`}>
          <slot />
        </div>
      </Host>
    );
  }

}
