import {SelectList} from "../../models";
import {AxiosInstance} from "axios";

export class RuntimeSelectListApi {
  private httpClient: AxiosInstance;

  constructor(httpClient: AxiosInstance) {
    this.httpClient = httpClient;
  }

  async get(providerTypeName: string, context?: any): Promise<SelectList> {
    const response = await this.httpClient.post('designer/runtime-select-list', {
      providerTypeName: providerTypeName,
      context: context
    });
    return response.data;
  }
}
