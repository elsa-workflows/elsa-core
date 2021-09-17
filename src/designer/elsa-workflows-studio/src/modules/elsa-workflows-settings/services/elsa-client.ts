import axios, {AxiosInstance, AxiosRequestConfig} from "axios";
import {Service} from 'axios-middleware';
import * as collection from 'lodash/collection';
import {eventBus} from '../../../services/event-bus';
import {EventTypes} from "../../../models";
import {WorkflowSettings} from "../models";


let _httpClient: AxiosInstance = null;
let _elsaWorkflowSettingsClient: ElsaWorkflowSettingsClient = null;

export const createHttpClient = function(baseAddress: string) : AxiosInstance
{
  if(!!_httpClient)
    return _httpClient;

  const config: AxiosRequestConfig = {
    baseURL: baseAddress
  };

  eventBus.emit(EventTypes.HttpClientConfigCreated, this, {config});

  const httpClient = axios.create(config);
  const service = new Service(httpClient);

  eventBus.emit(EventTypes.HttpClientCreated, this, {service, httpClient});

  return _httpClient = httpClient;
}

export const createElsaWorkflowSettingsClient = async function (serverUrl: string): Promise<ElsaWorkflowSettingsClient> {

  if (!!_elsaWorkflowSettingsClient)
    return _elsaWorkflowSettingsClient;

  const httpClient: AxiosInstance = await createHttpClient(serverUrl);

  _elsaWorkflowSettingsClient = {
    workflowSettingsApi: {
      list: async () => {
        const response = await httpClient.get<Array<WorkflowSettings>>(`v1/workflow-settings`);
        return response.data;
      },
      save: async request => {
        const response = await httpClient.post<WorkflowSettings>('v1/workflow-settings', request);
        return response.data;
      },
      delete: async id => {
        await httpClient.delete(`v1/workflow-settings/${id}`);
      },
    }
  }

  return _elsaWorkflowSettingsClient;
}

export interface ElsaWorkflowSettingsClient {
  workflowSettingsApi: WorkflowSettingsApi;
}

export interface WorkflowSettingsApi {

  list(): Promise<Array<WorkflowSettings>>;

  save(request: SaveWorkflowSettingsRequest): Promise<WorkflowSettings>;

  delete(workflowSettingsId: string): Promise<void>;
}

export interface SaveWorkflowSettingsRequest {
  id?: string;
  workflowBlueprintId?: string;
  key?: string;
  value?: string;
}
