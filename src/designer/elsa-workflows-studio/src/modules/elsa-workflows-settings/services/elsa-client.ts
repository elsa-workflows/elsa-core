import {AxiosInstance} from "axios";
import {WorkflowSettings} from "../models";
import {createHttpClient} from "../../../services/elsa-client"

let _elsaWorkflowSettingsClient: ElsaWorkflowSettingsClient = null;

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
