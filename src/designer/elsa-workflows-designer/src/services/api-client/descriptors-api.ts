import {ActivityDescriptorsApi, ActivityDescriptorsApiImpl} from "./activity-descriptors-api";
import {AxiosInstance} from "axios";
import {StorageDriversApi, StorageDriversApiImpl} from "./storage-drivers-api";

export interface DescriptorsApi {
  activities: ActivityDescriptorsApi;
  storageDrivers: StorageDriversApi;
}

export class DescriptorsApiImpl implements DescriptorsApi {
  httpClient: AxiosInstance;
  activities: ActivityDescriptorsApi;
  storageDrivers: StorageDriversApi;

  constructor(httpClient: AxiosInstance) {
    this.httpClient = httpClient;
    this.activities = new ActivityDescriptorsApiImpl(httpClient);
    this.storageDrivers = new StorageDriversApiImpl(httpClient);
  }

}
