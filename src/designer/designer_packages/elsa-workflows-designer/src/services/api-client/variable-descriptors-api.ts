import {AxiosInstance} from "axios";

export interface VariableDescriptorsResponse {
  items: Array<VariableDescriptor>;
}

export interface VariableDescriptor {
  typeName: string;
  displayName: string;
  category: string;
  description?: string;
}

export class VariableDescriptorsApi {
  private httpClient: AxiosInstance;

  constructor(httpClient: AxiosInstance) {
    this.httpClient = httpClient;
  }

  async list(): Promise<Array<VariableDescriptor>> {
    const response = await this.httpClient.get<VariableDescriptorsResponse>('descriptors/variables');
    return response.data.items;
  }
}
