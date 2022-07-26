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
