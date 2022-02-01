import 'reflect-metadata';
import {Service} from "typedi"
import {Port} from "../../../models";
import {PortProvider, PortProviderContext} from "./port-provider";

@Service()
export class DefaultPortProvider implements PortProvider {
  getInboundPorts(context: PortProviderContext): Array<Port> {
    const {activityDescriptor} = context;
    return [...activityDescriptor.inPorts];
  }

  getOutboundPorts(context: PortProviderContext): Array<Port> {
    const {activityDescriptor} = context;
    return [...activityDescriptor.outPorts];
  }

}
