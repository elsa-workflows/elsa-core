import {ActivityDescriptorsApi} from "./activity-descriptors-api";
import {AxiosInstance} from "axios";
import {StorageDriversApi} from "./storage-drivers-api";
import {VariableDescriptorsApi} from "./variable-descriptors-api";

export class DescriptorsApi {
  httpClient: AxiosInstance;
  activities: ActivityDescriptorsApi;
  storageDrivers: StorageDriversApi;
  variables: VariableDescriptorsApi;

  constructor(httpClient: AxiosInstance) {
    this.httpClient = httpClient;
    this.activities = new ActivityDescriptorsApi(httpClient);
    this.storageDrivers = new StorageDriversApi(httpClient);
    this.variables = new VariableDescriptorsApi(httpClient);
  }

}
