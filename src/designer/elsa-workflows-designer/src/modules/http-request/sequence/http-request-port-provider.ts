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

    const defaultPort = {name: 'unmatchedStatusCode', displayName: 'Unmatched status code', mode: PortMode.Embedded, isBrowsable: false}; // Hide the port from the designer until the editor uI is finished.
    const casesArray = this.getCases(activity);
    const ports = casesArray.map(x => ({name: x.statusCode.toString(), displayName: x.statusCode.toString(), mode: PortMode.Embedded}));

    return [...ports, defaultPort];
  }

  resolvePort(portName: string, context: PortProviderContext): Activity | Array<Activity> {
    const activity = context.activity as SendHttpRequest;
    const cases = this.getCases(activity);
    const matchingStatusCode = cases.find(x => x.statusCode.toString() == portName);
    return !matchingStatusCode ? activity.unmatchedStatusCode : matchingStatusCode.activity;
  }

  assignPort(portName: string, activity: Activity, context: PortProviderContext) {
    const sendHttpRequestActivity  = context.activity as SendHttpRequest;
    const cases = this.getCases(sendHttpRequestActivity);
    const matchingCase = cases.find(x => x.statusCode.toString() === portName);

    if(!matchingCase)
      return;

    matchingCase.activity = activity;
  }

  private getCases(activity: SendHttpRequest): Array<HttpStatusCodeCase> {
    const cases = activity.expectedStatusCodes;
    return !cases ? [] : cases;
  }
}
