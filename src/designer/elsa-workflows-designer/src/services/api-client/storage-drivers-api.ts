import {StorageDriverDescriptor} from "../../models";
import {AxiosInstance} from "axios";

export interface StorageDriversResponse {
  items: Array<StorageDriverDescriptor>;
}

export class StorageDriversApi {
  private httpClient: AxiosInstance;

  constructor(httpClient: AxiosInstance) {
    this.httpClient = httpClient;
  }

  async list(): Promise<Array<StorageDriverDescriptor>> {
    const response = await this.httpClient.get<StorageDriversResponse>('descriptors/storage-drivers');
    return response.data.items;
  }
}
