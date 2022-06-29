import 'reflect-metadata';
import {Service} from "typedi";
import {Port, PortMode} from "../../models";
import {SwitchActivity} from "./models";
import {PortProvider, PortProviderContext} from "../../services";

@Service()
export class SwitchPortProvider implements PortProvider {

  public getInboundPorts(context: PortProviderContext): Array<Port> {
    return [];
  }

  public getOutboundPorts(context: PortProviderContext): Array<Port> {
    const activity = context.activity as SwitchActivity;
    const cases = activity.cases;

    return cases.map(x => ({name: x.label, displayName: x.label, mode: PortMode.Embedded}));
  }
}
