import {Trigger, TriggerDescriptor} from "../models";

export interface TriggerDisplayContext {
  triggerDescriptor: TriggerDescriptor;
  displayType: TriggerDisplayType;
  trigger?: Trigger;
}

export interface TriggerDriver {
  display: (context: TriggerDisplayContext) => any;
}

export type TriggerDriverFactory = () => TriggerDriver;

export type TriggerDisplayType = string | 'designer' | 'picker';

