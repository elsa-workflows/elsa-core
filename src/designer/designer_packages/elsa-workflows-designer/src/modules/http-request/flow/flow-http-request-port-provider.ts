import 'reflect-metadata';
import {Service} from "typedi";
import {Activity, ActivityInput, InputDescriptor, ObjectExpression, Port, PortMode} from "../../../models";
import {PortProvider, PortProviderContext} from "../../../services";
import {FlowSendHttpRequest} from "./models";

@Service()
export class FlowHttpRequestPortProvider implements PortProvider {

  getOutboundPorts(context: PortProviderContext): Array<Port> {
    const activity = context.activity as FlowSendHttpRequest;

    if (activity == null)
      return [];

    const expectedStatusCodes = activity.expectedStatusCodes as ActivityInput;

    if(!expectedStatusCodes)
      return [];

    const statusCodesJson = (expectedStatusCodes.expression as ObjectExpression).value;
    const statusCodes = JSON.parse(statusCodesJson) as Array<string>;
    const catchAllPort = {name: 'Unmatched status code', displayName: 'Unmatched status code', mode: PortMode.Port};
    const outcomes = [...statusCodes.map(x => ({name: x.toString(), displayName: x.toString(), mode: PortMode.Port})), catchAllPort];

    return outcomes;
  }

  resolvePort(portName: string, context: PortProviderContext): Activity | Array<Activity> {
    return null;
  }

  assignPort(portName: string, activity: Activity, context: PortProviderContext) {
    return null;
  }
}
