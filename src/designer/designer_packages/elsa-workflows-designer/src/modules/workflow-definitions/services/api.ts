import {getVersionOptionsString, serializeQueryString} from '../../../utils';
import {InputDefinition, WorkflowDefinition, WorkflowDefinitionSummary, WorkflowOptions} from "../models/entities";
import {Activity, PagedList, Variable, VersionedEntity, VersionOptions} from "../../../models";
import {Service} from "typedi";
import {AxiosResponse} from "axios";
import {removeGuidsFromPortNames, addGuidsToPortNames} from '../../../utils/graph';
import {cloneDeep} from '@antv/x6/lib/util/object/object';
import {ElsaClientProvider} from "../../../services";

@Service()
export class WorkflowDefinitionsApi {
  private provider: ElsaClientProvider;

  constructor(provider: ElsaClientProvider) {
    this.provider = provider;
  }

  async publish(request: PublishWorkflowDefinitionRequest): Promise<WorkflowDefinition> {
    const httpClient = await this.getHttpClient();
    const response = await httpClient.post<WorkflowDefinition>(`workflow-definitions/${request.definitionId}/publish`);
    return response.data;
  }

  async retract(request: RetractWorkflowDefinitionRequest): Promise<WorkflowDefinition> {
    const httpClient = await this.getHttpClient();
    const response = await httpClient.post<WorkflowDefinition>(`workflow-definitions/${request.definitionId}/retract`, request);
    return response.data;
  }

  async delete(request: DeleteWorkflowDefinitionRequest): Promise<WorkflowDefinition> {
    const httpClient = await this.getHttpClient();
    const response = await httpClient.delete<WorkflowDefinition>(`workflow-definitions/${request.definitionId}`);
    return response.data;
  }

  async deleteVersion(request: DeleteWorkflowVersionRequest): Promise<WorkflowDefinition> {
    const httpClient = await this.getHttpClient();
    const response = await httpClient.delete<WorkflowDefinition>(`workflow-definitions/${request.definitionId}/version/${request.version}`);
    return response.data;
  }

  async revertVersion(request: RevertWorkflowVersionRequest): Promise<WorkflowDefinition> {
    const httpClient = await this.getHttpClient();
    const response = await httpClient.post<WorkflowDefinition>(`workflow-definitions/${request.definitionId}/revert/${request.version}`);
    return response.data;
  }

  async post(request: SaveWorkflowDefinitionRequest): Promise<WorkflowDefinition> {
    //TODO: Written as a workaround for different server and client models.
    //To be deleted after the port model on backend is updated.
    const requestClone = cloneDeep(request);
    removeGuidsFromPortNames(requestClone.model.root);

    const httpClient = await this.getHttpClient();
    const response = await httpClient.post<WorkflowDefinition>('workflow-definitions', requestClone);

    addGuidsToPortNames(response.data.root);
    return response.data;
  }

  async get(request: GetWorkflowRequest): Promise<WorkflowDefinition> {
    const queryString = {};

    if (!!request.versionOptions) queryString['versionOptions'] = getVersionOptionsString(request.versionOptions);
    if(request.includeCompositeRoot === true) queryString['includeCompositeRoot'] = true;

    const queryStringText = serializeQueryString(queryString);
    const httpClient = await this.getHttpClient();
    const response = await httpClient.get<WorkflowDefinition>(`workflow-definitions/${request.definitionId}${queryStringText}`);
    return response.data;
  }

  async getVersions(workflowDefinitionId: string): Promise<Array<WorkflowDefinition>> {
    const httpClient = await this.getHttpClient();
    const response = await httpClient.get<Array<WorkflowDefinition>>(`workflow-definitions/${workflowDefinitionId}/versions`);
    return response.data;
  }

  async list(request: ListWorkflowDefinitionsRequest): Promise<PagedList<WorkflowDefinitionSummary>> {
    const queryString: any = {};

    if (!!request.materializerName) queryString.materializer = request.materializerName;

    if (!!request.versionOptions) queryString.versionOptions = getVersionOptionsString(request.versionOptions);

    if (!!request.page) queryString.page = request.page;

    if (!!request.pageSize) queryString.pageSize = request.pageSize;

    if (!!request.pageSize) queryString.orderBy = request.orderBy;

    if (!!request.label) queryString.label = request.label;

    const queryStringText = serializeQueryString(queryString);
    const httpClient = await this.getHttpClient();
    const response = await httpClient.get<PagedList<WorkflowDefinitionSummary>>(`workflow-definitions${queryStringText}`);
    return response.data;
  }

  async export(request: ExportWorkflowRequest): Promise<ExportWorkflowResponse> {
    const queryString = {};

    if (!!request.versionOptions) queryString['versionOptions'] = getVersionOptionsString(request.versionOptions);

    const queryStringText = serializeQueryString(queryString);
    const definitionId = request.definitionId;
    const httpClient = await this.getHttpClient();

    const response = await httpClient.get(`workflow-definitions/${request.definitionId}/export${queryStringText}`, {
      responseType: 'blob',
    });

    const contentDispositionHeader = response.headers['content-disposition']; // Only available if the Elsa Server exposes the "Content-Disposition" header.
    const fileName = contentDispositionHeader ? contentDispositionHeader.split(';')[1].split('=')[1] : `workflow-definition-${definitionId}.json`;
    const data = response.data;

    return {
      fileName: fileName,
      data: data,
    };
  }

  async import(request: ImportWorkflowRequest): Promise<ImportWorkflowResponse> {
    const file = request.file;
    const definitionId = request.definitionId;
    const json = await file.text();
    const httpClient = await this.getHttpClient();
    let response: AxiosResponse;

    if (!definitionId) {
      response = await httpClient.put<WorkflowDefinition>(`workflow-definitions/import`, json, {
        headers: {
          'Content-Type': 'application/json',
        },
      });
    } else {
      response = await httpClient.post<WorkflowDefinition>(`workflow-definitions/${definitionId}/import`, json, {
        headers: {
          'Content-Type': 'application/json',
        },
      });
    }

    const workflowDefinition = response.data;

    // TODO: Written as a workaround for different server and client models.
    // To be deleted after the connection model on backend is updated.
    addGuidsToPortNames(workflowDefinition.root);

    return {workflowDefinition: workflowDefinition};
  }

  async deleteMany(request: DeleteManyWorkflowDefinitionRequest): Promise<DeleteManyWorkflowDefinitionResponse> {
    const httpClient = await this.getHttpClient();
    const response = await httpClient.post<DeleteManyWorkflowDefinitionResponse>(`bulk-actions/delete/workflow-definitions/by-definition-id`, {
      definitionIds: request.definitionIds,
    });
    return response.data;
  }

  async publishMany(request: PublishManyWorkflowDefinitionRequest): Promise<PublishManyWorkflowDefinitionResponse> {
    const httpClient = await this.getHttpClient();
    const response = await httpClient.post<PublishManyWorkflowDefinitionResponse>(`/bulk-actions/publish/workflow-definitions/by-definition-id`, {
      definitionIds: request.definitionIds,
    });
    return response.data;
  }

  async unpublishMany(request: UnpublishManyWorkflowDefinitionRequest): Promise<UnpublishManyWorkflowDefinitionResponse> {
    const httpClient = await this.getHttpClient();
    const response = await httpClient.post<UnpublishManyWorkflowDefinitionResponse>(`/bulk-actions/retract/workflow-definitions/by-definition-id`, {
      definitionIds: request.definitionIds,
    });
    return response.data;
  }

  async updateWorkflowReferences(request: UpdateWorkflowReferencesRequest): Promise<UpdateWorkflowReferencesResponse> {
    const httpClient = await this.getHttpClient();
    const response = await httpClient.post<UpdateWorkflowReferencesResponse>(`workflow-definitions/${request.definitionId}/update-references`, request);
    return response.data;
  }

  private getHttpClient = async () => await this.provider.getHttpClient();
}


export interface SaveWorkflowDefinitionRequest {
  model: WorkflowDefinition;
  publish: boolean;
}

export interface BaseManyWorkflowDefinitionRequest {
  definitionIds: string[];
}

export interface DeleteManyWorkflowDefinitionRequest extends BaseManyWorkflowDefinitionRequest {
}

export interface PublishManyWorkflowDefinitionRequest extends BaseManyWorkflowDefinitionRequest {
}

export interface UnpublishManyWorkflowDefinitionRequest extends BaseManyWorkflowDefinitionRequest {
}

export interface DeleteWorkflowDefinitionRequest {
  definitionId: string;
}

export interface DeleteWorkflowVersionRequest {
  definitionId: string;
  version: number;
}

export interface RevertWorkflowVersionRequest {
  definitionId: string;
  version: number;
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
  includeCompositeRoot?: boolean;
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
  definitionId?: string;
  file: File;
}

export interface ImportWorkflowResponse {
  workflowDefinition: WorkflowDefinition;
}

export enum WorkflowDefinitionsOrderBy {
  Name = 'Name',
  CreatedAt = 'CreatedAt',
}

export interface ListWorkflowDefinitionsRequest {
  page?: number;
  pageSize?: number;
  definitionIds?: Array<string>;
  versionOptions?: VersionOptions;
  materializerName?: string;
  orderBy?: WorkflowDefinitionsOrderBy;
  label?: Array<string>;
}

export interface DeleteManyWorkflowDefinitionResponse {
  deleted: number;
}

export interface PublishManyWorkflowDefinitionResponse {
  published: string[];
  alreadyPublished: string[];
  notFound: string[];
}

export interface UnpublishManyWorkflowDefinitionResponse {
  retracted: string[];
  notPublished: string[];
  notFound: string[];
}

export interface UpdateWorkflowReferencesRequest {
  definitionId: string;
  consumingWorkflowIds?: Array<string>;
}

export interface UpdateWorkflowReferencesResponse {
  affectedWorkflows: Array<string>;
}