import 'reflect-metadata';
import {h} from '@stencil/core';
import {Container, Service} from "typedi";
import {ActivityIconRegistry, PortProviderRegistry} from "../../../services";
import {Plugin} from "../../../models";
import {HttpRequestPortProvider} from "./http-request-port-provider";
import {HttpRequestIcon} from "../icons";

@Service()
export class HttpRequestPlugin implements Plugin {
  static readonly ActivityTypeName: string = 'Elsa.SendHttpRequest';

  constructor() {
    const activityTypeName = HttpRequestPlugin.ActivityTypeName;
    const portProviderRegistry = Container.get(PortProviderRegistry);
    const iconRegistry = Container.get(ActivityIconRegistry);

    portProviderRegistry.add(activityTypeName, () => Container.get(HttpRequestPortProvider));
    iconRegistry.add(HttpRequestPlugin.ActivityTypeName, settings => <HttpRequestIcon size={settings?.size}/>);
  }

  async initialize(): Promise<void> {

  }
}
