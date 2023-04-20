import {ModalType} from "./modal-type";

export interface ModalDialogInstance {
  content: () => any;
  options?: ShowModalDialogOptions;
  modalDialogRef?: HTMLElement;
  modalDialogContentRef?: HTMLElement;
  actionInvoked?: (args: ModalActionClickArgs) => void;
}

export interface ModalActionClickArgs {
  e: Event;
  action: ModalActionDefinition;
  instance: ModalDialogInstance
}

export interface ModalActionDefinition {
  text: string;
  name?: string;
  isPrimary?: boolean;
  isDangerous?: boolean;
  type?: ModalActionType;
  onClick?: (args: ModalActionClickArgs) => void;
  display?: (button: ModalActionDefinition) => any;
}

export enum ModalActionType {
  Button,
  Submit,
  Cancel
}

export interface ShowModalDialogOptions {
  actions?: Array<ModalActionDefinition>;
  modalType?: ModalType;
  autoHide?: boolean;
  size?: string;
}

export class DefaultModalActions {

  public static Cancel = (handler?: (args: ModalActionClickArgs) => void): ModalActionDefinition => ({
    text: 'Cancel',
    name: 'Cancel',
    type: ModalActionType.Cancel,
    onClick: handler
  });

  public static Close = (handler?: (args: ModalActionClickArgs) => void): ModalActionDefinition => ({
    text: 'Close',
    name: 'Close',
    type: ModalActionType.Cancel,
    onClick: handler
  });

  public static Save = (handler?: (args: ModalActionClickArgs) => void): ModalActionDefinition => ({
    text: 'Save',
    name: 'Save',
    type: ModalActionType.Submit,
    isPrimary: true,
    onClick: handler
  });

  public static Delete = (handler?: (args: ModalActionClickArgs) => void): ModalActionDefinition => ({
    text: 'Delete',
    name: 'Delete',
    type: ModalActionType.Button,
    isDangerous: true,
    onClick: handler
  });

  public static Publish = (handler?: (args: ModalActionClickArgs) => void): ModalActionDefinition => ({
    text: 'Publish',
    name: 'Publish',
    type: ModalActionType.Button,
    isDangerous: true,
    onClick: handler
  });

  public static Unpublish = (handler?: (args: ModalActionClickArgs) => void): ModalActionDefinition => ({
    text: 'Unpublish',
    name: 'Unpublish',
    type: ModalActionType.Button,
    isDangerous: true,
    onClick: handler
  });

  public static New = (handler?: (args: ModalActionClickArgs) => void): ModalActionDefinition => ({
    text: 'New',
    name: 'New',
    type: ModalActionType.Button,
    isPrimary: true,
    onClick: handler
  });

  public static Yes = (handler?: (args: ModalActionClickArgs) => void): ModalActionDefinition => ({
    text: 'Yes',
    name: 'Yes',
    type: ModalActionType.Button,
    isDangerous: true,
    onClick: handler
  });
}
