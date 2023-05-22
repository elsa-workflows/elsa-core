import {Component, h, Host} from '@stencil/core';
import state from './state';
import {ModalDialogInstance} from "./models";
import {ModalType} from "./modal-type";

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
          const options = instance.options;
          const actions = options?.actions ?? [];
          const modalType = options?.modalType ?? ModalType.Default;
          const size = options?.size ?? 'sm:tw-max-w-6xl';

          return (<elsa-modal-dialog
            ref={el => instance.modalDialogRef = el}
            type={modalType}
            size={size}
            modalDialogInstance={instance}
            content={instance.content}
            actions={actions}
            onActionInvoked={e => {
              const args = e.detail;
              instance.actionInvoked(args);
            }}
            onHidden={() => this.onInstanceHidden(instance)}/>);
        })}
      </Host>
    );
  }
}
