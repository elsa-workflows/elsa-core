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
        {instances.map(instance => {
          const actions = instance.actions ?? [];
          return (<elsa-modal-dialog type={instance.modalType} content={instance.content} actions={actions} onHidden={() => this.onInstanceHidden(instance)}/>);
        })}
      </Host>
    );
  }
}
