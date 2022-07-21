import {ActivityInputContext} from "../services/node-input-driver";
import {Activity, ActivityDescriptor} from "./core";

export interface TabDefinition {
  displayText: string;
  content: () => any;
  order?: number;
}

export interface TabChangedArgs {
  selectedTabIndex: number;
}

export enum ActionType {
  Button,
  Submit,
  Cancel
}

export type ActionClickArgs = (e: Event, action: ActionDefinition) => void;

export interface ActionDefinition {
  text: string;
  name?: string;
  isPrimary?: boolean;
  isDangerous?: boolean;
  type?: ActionType;
  onClick?: ActionClickArgs;
  display?: (button: ActionDefinition) => any;
}

export interface ActionInvokedArgs {
  action: ActionDefinition;
}

export class DefaultActions {

  public static Cancel = (handler?: ActionClickArgs): ActionDefinition => ({
    text: 'Cancel',
    name: 'Cancel',
    type: ActionType.Cancel,
    onClick: handler
  });

  public static Close = (handler?: ActionClickArgs): ActionDefinition => ({
    text: 'Close',
    name: 'Close',
    type: ActionType.Cancel,
    onClick: handler
  });

  public static Save = (handler?: ActionClickArgs): ActionDefinition => ({
    text: 'Save',
    name: 'Save',
    type: ActionType.Submit,
    isPrimary: true,
    onClick: handler
  });

  public static Delete = (handler?: ActionClickArgs): ActionDefinition => ({
    text: 'Delete',
    name: 'Delete',
    type: ActionType.Button,
    isDangerous: true,
    onClick: handler
  });

  public static New = (handler?: ActionClickArgs): ActionDefinition => ({
    text: 'New',
    name: 'New',
    type: ActionType.Button,
    isPrimary: true,
    onClick: handler
  });
}

export interface RenderActivityPropsContext {
  activity: Activity;
  activityDescriptor: ActivityDescriptor;
  title: string;
  inputs: Array<RenderActivityInputContext>;
}

export interface RenderActivityInputContext {
  inputContext: ActivityInputContext
  inputControl?: any;
}

export interface SelectList {
  items: Array<SelectListItem> | Array<string>;
  isFlagsEnum: boolean;
}

export interface SelectListItem {
  text: string;
  value: string;
}

export interface RuntimeSelectListProviderSettings {
  runtimeSelectListProviderType: string;
  context?: any;
}

export enum EditorHeight {
  Default = 'Default',
  Large = 'Large'
}
