import {AxiosInstance} from "axios";
import {Label, List} from "../../models";

export interface LabelsApi {
  list(): Promise<Array<Label>>;

  create(name: string, description?: string, color?: string): Promise<Label>;

  update(id: string, name: string, description?: string, color?: string): Promise<Label>;

  delete(id: string): Promise<boolean>;
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

  async create(name: string, description?: string, color?: string): Promise<Label> {
    const response = await this.httpClient.post<Label>('labels', {name, description, color});
    return response.data;
  }

  async update(id: string, name: string, description?: string, color?: string): Promise<Label> {
    const response = await this.httpClient.post<Label>(`labels/${id}`, {name, description, color});
    return response.data;
  }

  async delete(id: string): Promise<boolean> {
    const response = await this.httpClient.delete(`labels/${id}`);
    return response.status === 204;
  }
}
