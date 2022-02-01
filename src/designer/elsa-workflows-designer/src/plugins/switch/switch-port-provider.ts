import 'reflect-metadata';
import {Service} from "typedi";
import {PortProvider, PortProviderContext} from "../../components/activities/flowchart/port-provider";
import {Port} from "../../models";
import {SwitchActivity} from "./models";

@Service()
export class SwitchPortProvider implements PortProvider {

  public getInboundPorts(context: PortProviderContext): Array<Port> {
    return [{name: 'In', displayName: 'In'}];
  }

  public getOutboundPorts(context: PortProviderContext): Array<Port> {
    const activity = context.activity as SwitchActivity;
    const cases = activity.cases;

    return cases.map(x => ({name: x.label, displayName: x.label}));
  }
}
