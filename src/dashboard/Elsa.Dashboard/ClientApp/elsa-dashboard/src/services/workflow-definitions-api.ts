import { AxiosInstance } from 'axios';
import { AxiosFactory } from "./axios-factory";
import { Operation } from "fast-json-patch";
import { Workflow } from "@elsa-workflows/elsa-workflow-designer/dist/types/models";

export class WorkflowDefinitionsApi {

  client: AxiosInstance;

  constructor() {
    this.client = AxiosFactory.createDefaultClient();
  }

  list = async (): Promise<Array<Workflow>> => await this.get<Array<Workflow>>();
  getById = async (id: string, version?: number): Promise<Workflow> => {
    return await this.get<Workflow>(this.getVersionUrl(id, version));
  };

  post = async (workflow: Workflow): Promise<Workflow> => {
    const response = await this.client.post<Workflow>(this.getUrl(), workflow);
    return response.data;
  };

  patch = async (id: string, version: number, patch: Array<Operation>): Promise<Workflow> => {
    const response = await this.client.patch<Workflow>(this.getVersionUrl(id, version), patch);
    return response.data;
  };

  delete = async (id: string) => await this.client.delete(this.getUrl(id));

  private get = async <T = any>(relativePath: string = ''): Promise<T> => {
    const response = await this.client.get<T>(this.getUrl(relativePath));
    return response.data;
  };

  private getVersionUrl = (id: string, version: number): string => {
    let url = this.getUrl(id);

    if(!!version)
      url = `${url}?version=${ version }`;

    return url;
  };

  private getUrl = (relativePath: string = ''): string => {
    return `${ window._env_.API_URL }/api/workflow-definitions/${ relativePath }`;
  };
}

export default new WorkflowDefinitionsApi();
