import 'reflect-metadata';
import {Service} from "typedi";
import {Activity, Port, PortMode} from "../../models";
import {FlowSwitchActivity} from "./models";
import {PortProvider, PortProviderContext} from "../../services";

@Service()
export class FlowSwitchPortProvider implements PortProvider {

  getOutboundPorts(context: PortProviderContext): Array<Port> {
    const activity = context.activity as FlowSwitchActivity;

    if (activity == null)
      return [];

    const cases = activity.cases ?? [];
    return cases.map(x => ({name: x.label, displayName: x.label, mode: PortMode.Port}));
  }

  resolvePort(portName: string, context: PortProviderContext): Activity | Array<Activity> {
    debugger;
    return null;
  }

  assignPort(portName: string, activity: Activity, context: PortProviderContext) {
    debugger;
    return null;
  }
}
