import 'reflect-metadata';
import {Service} from "typedi";
import {camelCase} from 'lodash';
import {Activity, Port, PortMode} from "../../models";
import {SwitchActivity, SwitchCase} from "./models";
import {PortProvider, PortProviderContext} from "../../services";

@Service()
export class SwitchPortProvider implements PortProvider {

  getInboundPorts(context: PortProviderContext): Array<Port> {
    return [];
  }

  getOutboundPorts(context: PortProviderContext): Array<Port> {
    const activity = context.activity as SwitchActivity;

    if(activity == null)
      return [];

    const cases = activity.cases ?? [];
    return cases.map(x => ({name: x.label, displayName: x.label, mode: PortMode.Embedded}));
  }

  resolvePort(portName: string, context: PortProviderContext): Activity | Array<Activity> {
    const activity = context.activity as SwitchActivity;
    const cases: Array<SwitchCase> = activity.cases ?? [];
    const caseItem = cases.find(x => x.label == portName);

    if(!caseItem)
      return null;

    return caseItem.activity;
  }

  assignPort(portName: string, activity: Activity, context: PortProviderContext) {
    const switchActivity = context.activity as SwitchActivity;
    const cases: Array<SwitchCase> = switchActivity.cases ?? [];
    const caseItem = cases.find(x => x.label == portName);

    if(!caseItem)
      return null;

    caseItem.activity = activity;
  }
}
