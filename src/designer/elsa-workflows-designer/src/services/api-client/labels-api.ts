import {AxiosInstance} from "axios";
import {Label, List} from "../../models";

export interface LabelsApi {
  list(): Promise<Array<Label>>;
}

export class LabelsApiImpl implements LabelsApi {
  private httpClient: AxiosInstance;

  constructor(httpClient: AxiosInstance) {
    this.httpClient = httpClient;
  }

  async list(): Promise<Array<Label>> {
    const response = await this.httpClient.get<List<Label>>('labels');
    return response.data.items;
  }
}
