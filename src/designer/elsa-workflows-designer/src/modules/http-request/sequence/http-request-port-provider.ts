import 'reflect-metadata';
import {Service} from "typedi";
import {Activity, Port, PortMode} from "../../../models";
import {PortProvider, PortProviderContext} from "../../../services";
import {HttpStatusCodeCase, SendHttpRequest} from "./models";

@Service()
export class HttpRequestPortProvider implements PortProvider {

  getOutboundPorts(context: PortProviderContext): Array<Port> {
    const activity = context.activity as SendHttpRequest;

    if(activity == null)
      return [];

    const defaultPort = {name: 'unmatchedStatusCode', displayName: 'Unmatched status code', mode: PortMode.Embedded}; // Hide the port from the designer until the editor uI is finished.
    const statusCodes = activity.expectedStatusCodes ?? [];
    const ports = statusCodes.map(x => ({name: x.statusCode.toString(), displayName: x.statusCode.toString(), mode: PortMode.Embedded}));

    return [...ports, defaultPort];
  }

  resolvePort(portName: string, context: PortProviderContext): Activity | Array<Activity> {
    const activity = context.activity as SendHttpRequest;

    if(portName == 'unmatchedStatusCode')
      return activity.unmatchedStatusCode;

    const expectedStatusCodes: Array<HttpStatusCodeCase> = activity.expectedStatusCodes ?? [];
    const matchingStatusCode = expectedStatusCodes.find(x => x.statusCode.toString() == portName);
    return matchingStatusCode?.activity;
  }

  assignPort(portName: string, activity: Activity, context: PortProviderContext) {
    const sendHttpRequestActivity  = context.activity as SendHttpRequest;

    if(portName == 'unmatchedStatusCode') {
      sendHttpRequestActivity.unmatchedStatusCode = activity;
      return;
    }

    const statusCodes = sendHttpRequestActivity.expectedStatusCodes ?? [];
    const matchingStatusCode = statusCodes.find(x => x.statusCode.toString() === portName);

    if(!matchingStatusCode)
      return;

    matchingStatusCode.activity = activity;
  }
}
