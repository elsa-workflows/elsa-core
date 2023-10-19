import 'reflect-metadata';
import {Service} from "typedi";
import {Activity, Port, PortType} from "../../../models";
import {FlowSwitchActivity} from "./models";
import {PortProvider, PortProviderContext} from "../../../services";

@Service()
export class FlowSwitchPortProvider implements PortProvider {

  getOutboundPorts(context: PortProviderContext): Array<Port> {
    const activity = context.activity as FlowSwitchActivity;

    if (activity == null)
      return [];

    const cases = activity.cases ?? [];
    const ports = cases.map(x => ({name: x.label, displayName: x.label, type: PortType.Flow}));
    const defaultPort: Port = {name: 'Default', displayName: 'Default', type: PortType.Flow};
    return [...ports, defaultPort];
  }

  resolvePort(portName: string, context: PortProviderContext): Activity | Array<Activity> {
    return null;
  }

  assignPort(portName: string, activity: Activity, context: PortProviderContext) {
    return null;
  }
}
