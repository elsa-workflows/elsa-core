import 'reflect-metadata';
import {Service} from "typedi";
import {Activity, Port, PortType} from "../../../models";
import {SwitchActivity, SwitchCase} from "./models";
import {PortProvider, PortProviderContext} from "../../../services";

@Service()
export class SwitchPortProvider implements PortProvider {

  getOutboundPorts(context: PortProviderContext): Array<Port> {
    const activity = context.activity as SwitchActivity;

    if (activity == null)
      return [];

    const cases = activity.cases ?? [];
    const ports: Port[] = cases.map(x => ({name: x.label, displayName: x.label, type: PortType.Embedded}));
    const defaultPort: Port = {name: 'default', displayName: 'Default', type: PortType.Embedded};
    return [...ports, defaultPort];
  }

  resolvePort(portName: string, context: PortProviderContext): Activity | Array<Activity> {
    const activity = context.activity as SwitchActivity;

    if (portName == 'default')
      return activity.default;

    const cases: Array<SwitchCase> = activity.cases ?? [];
    const caseItem = cases.find(x => x.label == portName);

    if (!caseItem)
      return null;

    return caseItem.activity;
  }

  assignPort(portName: string, activity: Activity, context: PortProviderContext) {
    const switchActivity = context.activity as SwitchActivity;

    if (portName == 'default') {
      switchActivity.default = activity;
      return;
    }

    const cases: Array<SwitchCase> = switchActivity.cases ?? [];
    const caseItem = cases.find(x => x.label == portName);

    if (!caseItem)
      return;

    caseItem.activity = activity;
  }
}
