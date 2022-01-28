import {PortProvider, PortProviderContext} from "../../components/activities/flowchart/port-provider";
import {Port} from "../../models";

export class SwitchPortProvider implements PortProvider {
  getInboundPorts(context: PortProviderContext): Array<Port> {
    return undefined;
  }

  getOutboundPorts(context: PortProviderContext): Array<Port> {
    return undefined;
  }

}
