import axios, {AxiosInstance, AxiosRequestConfig} from "axios";
import {Service as MiddlewareService} from 'axios-middleware';
import {Container, Service} from 'typedi';
import {EventBus} from '../event-bus';
import 'reflect-metadata';
import {ServerSettings} from '../server-settings';
import {DescriptorsApi} from "./descriptors-api";
import {DesignerApi} from "./designer-api";
import {EventTypes} from "../../models";
import {ScriptingApi} from "./scripting-api";
import {FeaturesApi} from "./features-api";

export class ElsaClient {
  descriptors: DescriptorsApi;
  designer: DesignerApi;
  scripting: ScriptingApi;

  constructor(httpClient: AxiosInstance) {
    this.descriptors = new DescriptorsApi(httpClient);
    this.designer = new DesignerApi(httpClient);
    this.scripting = new ScriptingApi(httpClient);
  }
}

@Service()
export class ElsaClientProvider {
  private httpClient: AxiosInstance;
  private elsaClient: ElsaClient;

  constructor(private serverSettings: ServerSettings) {
  }

  public async getHttpClient(): Promise<AxiosInstance> {
    if (!!this.httpClient)
      return this.httpClient;

    return this.httpClient = await createHttpClient(this.serverSettings.baseAddress);
  }

  public async getElsaClient(): Promise<ElsaClient> {
    if (!!this.elsaClient)
      return this.elsaClient;

    const httpClient = await this.getHttpClient();
    return this.elsaClient = new ElsaClient(httpClient);
  }
}

async function createHttpClient(baseAddress: string): Promise<AxiosInstance> {
  const config: AxiosRequestConfig = {
    baseURL: baseAddress
  };

  const eventBus = Container.get(EventBus);
  await eventBus.emit(EventTypes.HttpClient.ConfigCreated, this, {config});
  const httpClient = axios.create(config);
  const middlewareService = new MiddlewareService(httpClient);
  await eventBus.emit(EventTypes.HttpClient.ClientCreated, this, {service: middlewareService, httpClient});

  return httpClient;
}
