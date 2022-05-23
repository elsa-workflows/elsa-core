import {Activity, PagedList, VersionOptions, WorkflowDefinition, WorkflowDefinitionSummary} from "../../models";
import {AxiosInstance} from "axios";
import {getVersionOptionsString, serializeQueryString} from "../../utils";

export interface WorkflowDefinitionsApi {
  post(request: SaveWorkflowDefinitionRequest): Promise<WorkflowDefinition>;

  get(request: GetWorkflowRequest): Promise<WorkflowDefinition>;

  list(request: ListWorkflowDefinitionsRequest): Promise<PagedList<WorkflowDefinitionSummary>>;

  delete(request: DeleteWorkflowDefinitionRequest): Promise<WorkflowDefinition>;

  retract(request: RetractWorkflowDefinitionRequest): Promise<WorkflowDefinition>;

  publish(request: PublishWorkflowDefinitionRequest): Promise<WorkflowDefinition>;

  export(request: ExportWorkflowRequest): Promise<ExportWorkflowResponse>;

  import(request: ImportWorkflowRequest): Promise<ImportWorkflowResponse>;
}

export interface SaveWorkflowDefinitionRequest {
  definitionId: string;
  name?: string;
  description?: string;
  publish: boolean;
  root?: Activity
}

export interface DeleteWorkflowDefinitionRequest {
  definitionId: string;
}

export interface RetractWorkflowDefinitionRequest {
  definitionId: string;
}

export interface PublishWorkflowDefinitionRequest {
  definitionId: string;
}

export interface GetWorkflowRequest {
  definitionId: string;
  versionOptions?: VersionOptions;
}

export interface ExportWorkflowRequest {
  definitionId: string;
  versionOptions?: VersionOptions;
}

export interface ExportWorkflowResponse {
  fileName: string;
  data: Blob;
}

export interface ImportWorkflowRequest {
  definitionId: string;
  file: File;
}

export interface ImportWorkflowResponse {
  workflowDefinition: WorkflowDefinition;
}

export interface ListWorkflowDefinitionsRequest {
  page?: number;
  pageSize?: number;
  definitionIds?: Array<string>;
  versionOptions?: VersionOptions;
  materializerName?: string;
}

export class WorkflowDefinitionsApiImpl implements WorkflowDefinitionsApi {
  private httpClient: AxiosInstance;

  constructor(httpClient: AxiosInstance) {
    this.httpClient = httpClient;
  }

  async publish(request: PublishWorkflowDefinitionRequest): Promise<WorkflowDefinition> {
    const response = await this.httpClient.post<WorkflowDefinition>(`workflow-definitions/${request.definitionId}/publish`);
    return response.data;
  }

  async retract(request: RetractWorkflowDefinitionRequest): Promise<WorkflowDefinition> {
    const response = await this.httpClient.post<WorkflowDefinition>(`workflow-definitions/${request.definitionId}/retract`);
    return response.data;
  }

  async delete(request: DeleteWorkflowDefinitionRequest): Promise<WorkflowDefinition> {
    const response = await this.httpClient.delete<WorkflowDefinition>(`workflow-definitions/${request.definitionId}`);
    return response.data;
  }

  async post(request: SaveWorkflowDefinitionRequest): Promise<WorkflowDefinition> {
    const response = await this.httpClient.post<WorkflowDefinition>('workflow-definitions', request);
    return response.data;
  }

  async get(request: GetWorkflowRequest): Promise<WorkflowDefinition> {
    const queryString = {};

    if (!!request.versionOptions)
      queryString['versionOptions'] = getVersionOptionsString(request.versionOptions);

    const queryStringText = serializeQueryString(queryString);
    const response = await this.httpClient.get<WorkflowDefinition>(`workflow-definitions/${request.definitionId}${queryStringText}`);
    return response.data;
  }

  async list(request: ListWorkflowDefinitionsRequest): Promise<PagedList<WorkflowDefinitionSummary>> {
    const queryString: any = {};

    if (!!request.materializerName)
      queryString.materializer = request.materializerName;

    if (!!request.versionOptions)
      queryString.versionOptions = getVersionOptionsString(request.versionOptions);

    if (!!request.page)
      queryString.page = request.page;

    if (!!request.pageSize)
      queryString.pageSize = request.pageSize;

    const queryStringText = serializeQueryString(queryString);
    const response = await this.httpClient.get<PagedList<WorkflowDefinitionSummary>>(`workflow-definitions${queryStringText}`);
    return response.data;
  }

  async export(request: ExportWorkflowRequest): Promise<ExportWorkflowResponse> {
    const queryString = {};

    if (!!request.versionOptions)
      queryString['versionOptions'] = getVersionOptionsString(request.versionOptions);

    const queryStringText = serializeQueryString(queryString);
    const definitionId = request.definitionId;

    const response = await this.httpClient.get(`workflow-definitions/${request.definitionId}/export${queryStringText}`, {
      responseType: "blob"
    });

    const contentDispositionHeader = response.headers["content-disposition"]; // Only available if the Elsa Server exposes the "Content-Disposition" header.
    const fileName = contentDispositionHeader ? contentDispositionHeader.split(";")[1].split("=")[1] : `workflow-definition-${definitionId}.json`;
    const data = response.data;

    return {
      fileName: fileName,
      data: data
    };
  }

  async import(request: ImportWorkflowRequest): Promise<ImportWorkflowResponse> {
    const file = request.file;
    const definitionId = request.definitionId;
    const json = await file.text();

    const response = await this.httpClient.post<WorkflowDefinition>(`workflow-definitions/${definitionId}/import`, json, {
      headers: {
        'Content-Type': 'application/json'
      }
    });

    const workflowDefinition = response.data;
    return {workflowDefinition: workflowDefinition};
  }
}
