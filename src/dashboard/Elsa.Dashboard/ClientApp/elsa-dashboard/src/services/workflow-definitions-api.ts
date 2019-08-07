import { AxiosInstance } from 'axios';
import { AxiosFactory } from "./axios-factory";
import { Operation } from "fast-json-patch";
import { Workflow } from "@elsa-workflows/elsa-workflow-designer/dist/types/models";

export class WorkflowDefinitionsApi {

  client: AxiosInstance;

  constructor() {
    this.client = AxiosFactory.createDefaultClient();
  }

  list = async (): Promise<Array<Workflow>> => await this.get<Array<Workflow>>(this.getUrl());

  getById = async (id: string): Promise<Workflow> => {
    return await this.get<Workflow>(this.getUrl(id));
  };

  post = async (workflow: Workflow): Promise<Workflow> => {
    const response = await this.client.post<Workflow>(this.getUrl(), workflow);
    return response.data;
  };

  patch = async (id: string, patch: Array<Operation>): Promise<Workflow> => {
    const response = await this.client.patch<Workflow>(this.getUrl(id), patch);
    return response.data;
  };

  publish = async (id: string): Promise<Workflow> => {
    const response = await this.client.post(this.getUrl(`${id}/publish`));
    return response.data;
  };

  delete = async (id: string) => await this.client.delete(this.getUrl(id));

  private get = async <T = any>(url): Promise<T> => {
    const response = await this.client.get<T>(url);
    return response.data;
  };

  private getUrl = (relativePath: string = ''): string => {
    return `${ window._env_.API_URL }/api/workflow-definitions/${ relativePath }`;
  };
}

export default new WorkflowDefinitionsApi();
