import {ActivityDescriptorsApi, ActivityDescriptorsApiImpl} from "./activity-descriptors-api";
import {AxiosInstance} from "axios";

export interface DescriptorsApi {
  activities: ActivityDescriptorsApi;
}

export class DescriptorsApiImpl implements DescriptorsApi {
  httpClient: AxiosInstance;
  activities: ActivityDescriptorsApi;

  constructor(httpClient: AxiosInstance) {
    this.httpClient = httpClient;
    this.activities = new ActivityDescriptorsApiImpl(httpClient);
  }

}
