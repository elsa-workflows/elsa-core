import {Activity, ActivityDescriptor, Port} from "../models";

export interface PortProvider {
  getOutboundPorts(context: PortProviderContext): Array<Port>;

  resolvePort(portName: string, context: PortProviderContext): Activity | Array<Activity>;

  assignPort(portName: string, activity: Activity, context: PortProviderContext);
}

export interface PortProviderContext {
  activityDescriptor: ActivityDescriptor;
  activity: Activity;
}
