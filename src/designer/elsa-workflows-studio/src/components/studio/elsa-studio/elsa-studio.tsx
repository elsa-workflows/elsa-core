import {Component, Host, h, Listen, Event, EventEmitter} from '@stencil/core';

@Component({
  tag: 'elsa-studio',
  styleUrl: 'elsa-studio.css',
  shadow: false,
})
export class ElsaWorkflowStudio {
  render() {
    return (
      <Host>
        <slot />
      </Host>
    );
  }

}
