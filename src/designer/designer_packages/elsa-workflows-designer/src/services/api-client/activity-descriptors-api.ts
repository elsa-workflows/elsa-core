import {ActivityDescriptor} from "../../models";
import {AxiosInstance} from "axios";

export interface ActivityDescriptorResponse {
  items: Array<ActivityDescriptor>;
}

export class ActivityDescriptorsApi {
  private httpClient: AxiosInstance;

  constructor(httpClient: AxiosInstance) {
    this.httpClient = httpClient;
  }

  async list(): Promise<Array<ActivityDescriptor>> {
    const response = await this.httpClient.get<ActivityDescriptorResponse>('descriptors/activities');
    return response.data.items;
  }
}
