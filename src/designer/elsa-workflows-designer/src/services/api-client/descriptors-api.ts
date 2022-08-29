import {ActivityDescriptorsApi} from "./activity-descriptors-api";
import {AxiosInstance} from "axios";
import {StorageDriversApi} from "./storage-drivers-api";

export class DescriptorsApi {
  httpClient: AxiosInstance;
  activities: ActivityDescriptorsApi;
  storageDrivers: StorageDriversApi;

  constructor(httpClient: AxiosInstance) {
    this.httpClient = httpClient;
    this.activities = new ActivityDescriptorsApi(httpClient);
    this.storageDrivers = new StorageDriversApi(httpClient);
  }

}
