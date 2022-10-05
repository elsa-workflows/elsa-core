import {ActivityModel, ActivityDescriptor, Port} from "../models";

export interface PortProvider {
  getOutboundPorts(context: PortProviderContext): Array<Port>;

  resolvePort(portName: string, context: PortProviderContext): ActivityModel | Array<ActivityModel>;

  assignPort(portName: string, activity: ActivityModel, context: PortProviderContext);
}

export interface PortProviderContext {
  activityDescriptor: ActivityDescriptor;
  activity: ActivityModel;
}
