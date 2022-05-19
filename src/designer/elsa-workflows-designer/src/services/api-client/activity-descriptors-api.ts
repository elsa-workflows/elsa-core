import {ActivityDescriptor} from "../../models";
import {AxiosInstance} from "axios";

export interface ActivityDescriptorsApi {
  list(): Promise<Array<ActivityDescriptor>>;
}

export interface ActivityDescriptorResponse {
  activityDescriptors: Array<ActivityDescriptor>;
}

export class ActivityDescriptorsApiImpl implements ActivityDescriptorsApi {
  private httpClient: AxiosInstance;

  constructor(httpClient: AxiosInstance) {
    this.httpClient = httpClient;
  }

  async list(): Promise<Array<ActivityDescriptor>> {
    const response = await this.httpClient.get<ActivityDescriptorResponse>('descriptors/activities');
    return response.data.activityDescriptors;
  }
}
