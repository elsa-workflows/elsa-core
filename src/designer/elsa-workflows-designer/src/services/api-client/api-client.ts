import axios, {AxiosInstance, AxiosRequestConfig} from "axios";
import {Service as MiddlewareService} from 'axios-middleware';
import {Container, Service} from 'typedi';
import {EventBus} from '../event-bus';
import 'reflect-metadata';
import {ServerSettings} from '../server-settings';
import {WorkflowDefinitionsApi, WorkflowDefinitionsApiImpl} from "./workflow-definitions-api";
import {WorkflowInstancesApi, WorkflowInstancesApiImpl} from "./workflow-instances-api";
import {DescriptorsApi, DescriptorsApiImpl} from "./descriptors-api";
import {DesignerApi, DesignerApiImpl} from "./designer-api";
import {EventTypes} from "../../models";

export class ElsaClient {
  httpClient: AxiosInstance;
  descriptors: DescriptorsApi;
  designer: DesignerApi;
  workflowDefinitions: WorkflowDefinitionsApi;
  workflowInstances: WorkflowInstancesApi;

  constructor(httpClient: AxiosInstance) {
    this.httpClient = httpClient;
    this.workflowDefinitions = new WorkflowDefinitionsApiImpl(httpClient);
    this.workflowInstances = new WorkflowInstancesApiImpl(httpClient);
    this.descriptors = new DescriptorsApiImpl(httpClient);
    this.designer = new DesignerApiImpl(httpClient);
  }
}

@Service()
export class ElsaApiClientProvider {
  private httpClient: AxiosInstance;
  private elsaClient: ElsaClient;

  constructor(private serverSettings: ServerSettings) {
  }

  public async getHttpClient(): Promise<AxiosInstance>
  {
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
