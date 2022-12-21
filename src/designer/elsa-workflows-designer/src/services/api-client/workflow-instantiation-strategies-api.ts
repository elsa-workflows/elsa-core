import {List, StorageDriverDescriptor, WorkflowInstantiationStrategyDescriptor} from "../../models";
import {AxiosInstance} from "axios";

export class WorkflowInstantiationStrategiesApi {
  private httpClient: AxiosInstance;

  constructor(httpClient: AxiosInstance) {
    this.httpClient = httpClient;
  }

  async list(): Promise<Array<WorkflowInstantiationStrategyDescriptor>> {
    const response = await this.httpClient.get<List<WorkflowInstantiationStrategyDescriptor>>('descriptors/workflow-instantiation-strategies');
    return response.data.items;
  }
}
