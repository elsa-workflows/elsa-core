import axios, {AxiosInstance, AxiosRequestConfig} from "axios";
import {Service as MiddlewareService} from 'axios-middleware';
import {Container, Service} from 'typedi';
import {EventBus} from '../event-bus';
import 'reflect-metadata';
import {ServerSettings} from '../server-settings';
import {WorkflowDefinitionsApi, WorkflowDefinitionsApiImpl} from "./workflow-definitions-api";
import {WorkflowInstancesApi, WorkflowInstancesApiImpl} from "./workflow-instances-api";
import {DescriptorsApi, DescriptorsApiImpl} from "./descriptors-api";
import {LabelsApi, LabelsApiImpl} from "./labels-api";
import {DesignerApi, DesignerApiImpl} from "./designer-api";
import {EventTypes} from "../../models";

export interface ElsaClient {
  descriptors: DescriptorsApi;
  workflowDefinitions: WorkflowDefinitionsApi;
  workflowInstances: WorkflowInstancesApi;
  labels: LabelsApi;
  designer: DesignerApi;
}

class ElsaClientImpl implements ElsaClient {
  httpClient: AxiosInstance;
  descriptors: DescriptorsApi;
  designer: DesignerApi;
  labels: LabelsApi;
  workflowDefinitions: WorkflowDefinitionsApi;
  workflowInstances: WorkflowInstancesApi;

  constructor(httpClient: AxiosInstance) {
    this.httpClient = httpClient;
    this.workflowDefinitions = new WorkflowDefinitionsApiImpl(httpClient);
    this.workflowInstances = new WorkflowInstancesApiImpl(httpClient);
    this.descriptors = new DescriptorsApiImpl(httpClient);
    this.designer = new DesignerApiImpl(httpClient);
    this.labels = new LabelsApiImpl(httpClient);
  }
}

@Service()
export class ElsaApiClientProvider {
  private elsaClient: ElsaClient;

  constructor(private serverSettings: ServerSettings) {
  }

  public async getClient(): Promise<ElsaClient> {
    if (!!this.elsaClient)
      return this.elsaClient;

    return this.elsaClient = await createElsaClient(this.serverSettings.baseAddress);
  }
}

export async function createHttpClient(baseAddress: string): Promise<AxiosInstance> {
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

export async function createElsaClient(serverUrl: string): Promise<ElsaClient> {

  const httpClient: AxiosInstance = await createHttpClient(serverUrl);

  return new ElsaClientImpl(httpClient);
}
