import {Component, Host, h} from '@stencil/core';
import state from './state';
import {ModalDialogInstance} from "./models";

@Component({
  tag: 'elsa-modal-dialog-container',
  shadow: false,
})
export class ModalDialogContainer {

  private onInstanceHidden = (instance: ModalDialogInstance) => {
    let instances: Array<ModalDialogInstance> = state.instances;
    instances = instances.filter(x => x != instance);
    state.instances = instances;
  }

  render() {
    const instances: Array<ModalDialogInstance> = state.instances;

    return (
      <Host>
        {instances.map(instance => <elsa-modal-dialog content={instance.content} onHidden={() => this.onInstanceHidden(instance)}/>)}
      </Host>
    );
  }
}
