import {Activity, ActivityDescriptor, Port} from "../models";

export interface PortProvider {
  getInboundPorts(context: PortProviderContext): Array<Port>;
  getOutboundPorts(context: PortProviderContext): Array<Port>;
}

export interface PortProviderContext {
  activityDescriptor: ActivityDescriptor;
  activity: Activity;
}
