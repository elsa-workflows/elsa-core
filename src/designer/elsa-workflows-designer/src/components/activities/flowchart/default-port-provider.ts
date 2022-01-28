import 'reflect-metadata';
import {Service} from "typedi"
import {Port} from "../../../models";
import {PortProvider, PortProviderContext} from "./port-provider";

@Service()
export class DefaultPortProvider implements PortProvider {
  getInboundPorts(context: PortProviderContext): Array<Port> {
    const {activityDescriptor} = context;
    let inPorts: Array<Port> = [...activityDescriptor.inPorts];

    if (inPorts.length == 0)
      inPorts = [{name: 'In', displayName: 'In'}];

    if (inPorts.length == 1)
      inPorts[0].displayName = null;

    return inPorts;
  }

  getOutboundPorts(context: PortProviderContext): Array<Port> {
    const {activityDescriptor} = context;
    let outPorts: Array<Port> = [...activityDescriptor.outPorts];

    outPorts = [...outPorts, {name: 'Done', displayName: 'Done'}];

    if (outPorts.length == 1)
      outPorts[0].displayName = null;

    return outPorts;
  }

}
