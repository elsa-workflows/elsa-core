import {Component, h, Host } from '@stencil/core';

@Component({
  tag: 'elsa-shell'
})
export class Shell {

  render() {
    return <Host>
      <slot></slot>
    </Host>;
  }
}
