import 'reflect-metadata';
import {h} from '@stencil/core';
import {Container, Service} from "typedi";
import {ActivityIconRegistry, PortProviderRegistry} from "../../../services";
import {Plugin} from "../../../models";
import {FlowHttpRequestPortProvider} from "./flow-http-request-port-provider";
import {HttpRequestIcon} from "../icons";

@Service()
export class FlowHttpRequestPlugin implements Plugin {
  static readonly ActivityTypeName: string = 'Elsa.FlowSendHttpRequest';

  constructor() {
    const activityTypeName = FlowHttpRequestPlugin.ActivityTypeName;
    const portProviderRegistry = Container.get(PortProviderRegistry);
    const iconRegistry = Container.get(ActivityIconRegistry);

    portProviderRegistry.add(activityTypeName, () => Container.get(FlowHttpRequestPortProvider));
    iconRegistry.add(FlowHttpRequestPlugin.ActivityTypeName, settings => <HttpRequestIcon size={settings?.size}/>);
  }

  async initialize(): Promise<void> {

  }
}
