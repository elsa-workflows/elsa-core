import 'reflect-metadata';
import {Service} from "typedi"
import {Port} from "../../../models";
import {PortProvider, PortProviderContext} from "./port-provider";

@Service()
export class FlowchartPortProvider implements PortProvider {
  getInboundPorts(context: PortProviderContext): Array<Port> {
    return [];
  }

  getOutboundPorts(context: PortProviderContext): Array<Port> {
    return [];
  }

}
