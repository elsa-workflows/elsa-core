import 'reflect-metadata';
import {Service} from "typedi"
import {Port} from "../../../models";
import {PortProvider, PortProviderContext} from "./port-provider";

@Service()
export class DefaultPortProvider implements PortProvider {
  getInboundPorts(context: PortProviderContext): Array<Port> {
    const {activityDescriptor} = context;
    return [];
  }

  getOutboundPorts(context: PortProviderContext): Array<Port> {
    const {activityDescriptor} = context;
    return [...activityDescriptor.ports];
  }

}
