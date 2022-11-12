import 'reflect-metadata';
import {Service} from "typedi";
import state from "./state";
import {ModalActionDefinition, ModalDialogInstance} from "./models";
import {ModalType} from "../../../components/shared/modal-dialog";

@Service()
export class ModalDialogService {

  show(content: () => any, actions?: Array<ModalActionDefinition>, modalType?: ModalType, autoHide?: boolean): ModalDialogInstance {
    const newInstance: ModalDialogInstance = {
      content: content,
      actions: actions ?? [],
      modalType: modalType ?? ModalType.Default,
      autoHide: autoHide ?? true
    };

    let instances: Array<ModalDialogInstance> = state.instances;
    instances = [...instances, newInstance];
    state.instances = instances;
    return newInstance;
  }

  hide(instance: ModalDialogInstance) {
    let instances: Array<ModalDialogInstance> = state.instances;
    instances = instances.filter(x => x != instance);
    state.instances = instances;
  }
}
