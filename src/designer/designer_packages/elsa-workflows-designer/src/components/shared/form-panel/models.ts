export interface PanelActionClickArgs {
  e: Event;
  action: PanelActionDefinition;
}

export interface PanelActionDefinition {
  text: string;
  name?: string;
  isPrimary?: boolean;
  isDangerous?: boolean;
  type?: PanelActionType;
  onClick?: (args: PanelActionClickArgs) => void;
  display?: (button: PanelActionDefinition) => any;
}

export enum PanelActionType {
  Button,
  Submit,
  Cancel
}
