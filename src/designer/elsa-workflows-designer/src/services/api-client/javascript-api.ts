import {AxiosInstance} from "axios";

export class JavaScriptApi {

  constructor(private httpClient: AxiosInstance) {
  }

  async getTypeDefinitions(request: GetTypeDefinitionsRequest): Promise<string> {
    const response = await this.httpClient.post(`scripting/javascript/type-definitions/${request.containerId}`, request);
    return response.data;
  }
}

export interface GetTypeDefinitionsRequest {
  containerType: string;
  containerId: string;
  activityTypeName?: string;
  propertyName?: string;
}
