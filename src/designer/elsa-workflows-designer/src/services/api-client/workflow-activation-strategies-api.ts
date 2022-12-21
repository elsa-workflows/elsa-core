import {List, StorageDriverDescriptor, WorkflowActivationStrategyDescriptor} from "../../models";
import {AxiosInstance} from "axios";

export class WorkflowActivationStrategiesApi {
  private httpClient: AxiosInstance;

  constructor(httpClient: AxiosInstance) {
    this.httpClient = httpClient;
  }

  async list(): Promise<Array<WorkflowActivationStrategyDescriptor>> {
    const response = await this.httpClient.get<List<WorkflowActivationStrategyDescriptor>>('descriptors/workflow-activation-strategies');
    return response.data.items;
  }
}
