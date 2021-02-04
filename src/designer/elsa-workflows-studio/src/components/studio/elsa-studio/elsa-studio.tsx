import {Component, Host, h, Listen, Event, EventEmitter} from '@stencil/core';
import {eventBus} from '../../../utils/event-bus';

@Component({
  tag: 'elsa-studio',
  styleUrl: 'elsa-studio.css',
  shadow: false,
})
export class ElsaWorkflowStudio {

  @Event({bubbles: true, composed: true, eventName: 'debug'}) debug: EventEmitter<AddActivityEventArgs>

  @Listen('add-activity')
  addActivityRequestHandler(event: CustomEvent<AddActivityEventArgs>) {
    console.debug('Received the custom event: ', event.detail.sourceActivityId);
    eventBus.emit('add-activity');
  }

  render() {
    return (
      <Host>
        <slot />
      </Host>
    );
  }

}
