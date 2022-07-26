import {ActionDefinition} from "../../../models";

export interface ModalDialogInstance {
  content: () => any;
  actions: Array<ActionDefinition>;
}
