import {Component, Host, h, Prop, State, Event, EventEmitter, Watch} from '@stencil/core';
import {enter, leave, toggle} from 'el-transition'
import {registerClickOutside} from "stencil-click-outside";
import {WebhookDefinition} from "../../../../models/webhook";
import {createElsaClient} from "../../../../services/elsa-client";
import Tunnel from '../../../../data/workflow-editor';

@Component({
  tag: 'elsa-webhook-save-button',
  shadow: false,
})
export class ElsaWebhookSaveButton {

  @Prop() webhookDefinition: WebhookDefinition;
  @Prop() saving: boolean;
  @Event({bubbles: true}) saveClicked: EventEmitter;

  //menu: HTMLElement;

  onSaveClick() {
    this.saveClicked.emit();
    //leave(this.menu);
  }

  render() {
    return (
      
        <span class="elsa-relative elsa-z-0 elsa-inline-flex elsa-shadow-sm elsa-rounded-md">
          {this.saving ? this.renderSavingButton() : this.renderSaveButton()}
          <span class="-elsa-ml-px elsa-relative elsa-block">
        </span>
        </span>

    );
  }

  renderSaveButton() {
    return (
      <button type="button"
              onClick={() => this.onSaveClick()}
              class="elsa-relative elsa-inline-flex elsa-items-center elsa-px-4 elsa-py-2 elsa-rounded-l-md elsa-border elsa-border-gray-300 elsa-bg-white elsa-text-sm elsa-font-medium elsa-text-gray-700 hover:elsa-bg-gray-50 focus:elsa-z-10 focus:elsa-outline-none focus:elsa-ring-1 focus:elsa-ring-blue-500 focus:elsa-border-blue-500">

        Save
      </button>);
  }

  renderSavingButton() {
    return (
      <button type="button"
              disabled={true}
              class="elsa-relative elsa-inline-flex elsa-items-center elsa-px-4 elsa-py-2 elsa-rounded-l-md elsa-border elsa-border-gray-300 elsa-bg-white elsa-text-sm elsa-font-medium elsa-text-gray-700 hover:elsa-bg-gray-50 focus:elsa-z-10 focus:elsa-outline-none focus:elsa-ring-1 focus:elsa-ring-blue-500 focus:elsa-border-blue-500">

        <svg class="elsa-animate-spin -elsa-ml-1 elsa-mr-3 elsa-h-5 elsa-w-5 elsa-text-blue-400" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
          <circle class="elsa-opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
          <path class="elsa-opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"/>
        </svg>
        Saving
      </button>);
  }
}

Tunnel.injectProps(ElsaWebhookSaveButton, ['serverUrl']);
