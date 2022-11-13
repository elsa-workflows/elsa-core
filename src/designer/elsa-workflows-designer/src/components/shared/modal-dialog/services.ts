import 'reflect-metadata';
import {Service} from "typedi";
import state from "./state";
import {ModalActionDefinition, ModalDialogInstance, ShowModalDialogOptions} from "./models";
import {ModalType} from "../../../components/shared/modal-dialog";

@Service()
export class ModalDialogService {

  show(content: () => any, options?: ShowModalDialogOptions): ModalDialogInstance {
    const newInstance: ModalDialogInstance = {
      content: content,
      options: options
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
