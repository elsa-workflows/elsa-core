import 'reflect-metadata';
import {Service} from "typedi";
import state from "./state";
import {ActionDefinition, ModalDialogInstance} from "./models";

@Service()
export class ModalDialogService {

  show(content: () => any, actions?: Array<ActionDefinition>): ModalDialogInstance {
    const newInstance: ModalDialogInstance = {
      content: content,
      actions: []
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
