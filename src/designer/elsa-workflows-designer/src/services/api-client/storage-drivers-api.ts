import {ActivityDescriptor, StorageDriverDescriptor} from "../../models";
import {AxiosInstance} from "axios";

export interface StorageDriversApi {
  list(): Promise<Array<StorageDriverDescriptor>>;
}

export interface StorageDriversResponse {
  items: Array<StorageDriverDescriptor>;
}

export class StorageDriversApiImpl implements StorageDriversApi {
  private httpClient: AxiosInstance;

  constructor(httpClient: AxiosInstance) {
    this.httpClient = httpClient;
  }

  async list(): Promise<Array<StorageDriverDescriptor>> {
    const response = await this.httpClient.get<StorageDriversResponse>('descriptors/storage-drivers');
    return response.data.items;
  }
}
