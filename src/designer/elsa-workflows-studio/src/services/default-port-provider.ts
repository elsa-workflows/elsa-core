import 'reflect-metadata';
import {camelCase} from 'lodash';
import {Service} from "typedi"
import {PortProvider, PortProviderContext} from "./port-provider";
import {ActivityModel, Port} from "../models";

@Service()
export class DefaultPortProvider implements PortProvider {
  getOutboundPorts(context: PortProviderContext): Array<Port> {
    const {activityDescriptor} = context;
    return [...(activityDescriptor.ports || [])];
  }

  resolvePort(portName: string, context: PortProviderContext): ActivityModel | Array<ActivityModel> {
    const propName = camelCase(portName);
    const activity = context.activity;

    if (!activity)
      return null;

    return activity[propName] as ActivityModel | Array<ActivityModel>;
  }

  assignPort(portName: string, activity: ActivityModel, context: PortProviderContext) {
    const propName = camelCase(portName);
    const container = context.activity;

    if (!container)
      return null;

    container[propName] = activity;
  }
}
