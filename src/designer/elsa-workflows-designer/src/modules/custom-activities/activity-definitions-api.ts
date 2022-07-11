import {List, PagedList} from "../../models";
import {Service} from "typedi";
import {getVersionOptionsString, serializeQueryString} from "../../utils";
import {
  DeleteManyActivityDefinitionRequest,
  DeleteManyActivityDefinitionResponse,
  DeleteActivityDefinitionRequest,
  ExportWorkflowRequest,
  ExportWorkflowResponse,
  GetWorkflowRequest,
  ImportWorkflowRequest,
  ImportWorkflowResponse,
  ListActivityDefinitionsRequest,
  PublishManyActivityDefinitionRequest,
  PublishManyActivityDefinitionResponse,
  PublishActivityDefinitionRequest,
  RetractActivityDefinitionRequest,
  SaveActivityDefinitionRequest,
  UnpublishManyActivityDefinitionRequest,
  UnpublishManyActivityDefinitionResponse,
  ActivityDefinition,
  ActivityDefinitionSummary
} from "./models";
import {ElsaApiClientProvider} from "../../services";

@Service()
export class ActivityDefinitionsApi {
  private provider: ElsaApiClientProvider;

  constructor(provider: ElsaApiClientProvider) {
    this.provider = provider;
  }

  async publish(request: PublishActivityDefinitionRequest): Promise<ActivityDefinition> {
    const httpClient = await this.provider.getHttpClient();
    const response = await httpClient.post<ActivityDefinition>(`workflow-definitions/${request.definitionId}/publish`);
    return response.data;
  }

  async retract(request: RetractActivityDefinitionRequest): Promise<ActivityDefinition> {
    const httpClient = await this.provider.getHttpClient();
    const response = await httpClient.post<ActivityDefinition>(`workflow-definitions/${request.definitionId}/retract`);
    return response.data;
  }

  async delete(request: DeleteActivityDefinitionRequest): Promise<ActivityDefinition> {
    const httpClient = await this.provider.getHttpClient();
    const response = await httpClient.delete<ActivityDefinition>(`workflow-definitions/${request.definitionId}`);
    return response.data;
  }

  async post(request: SaveActivityDefinitionRequest): Promise<ActivityDefinition> {
    const httpClient = await this.provider.getHttpClient();
    const response = await httpClient.post<ActivityDefinition>('workflow-definitions', request);
    return response.data;
  }

  async get(request: GetWorkflowRequest): Promise<ActivityDefinition> {
    const queryString = {};

    if (!!request.versionOptions) queryString['versionOptions'] = getVersionOptionsString(request.versionOptions);

    const queryStringText = serializeQueryString(queryString);
    const httpClient = await this.provider.getHttpClient();
    const response = await httpClient.get<ActivityDefinition>(`workflow-definitions/${request.definitionId}${queryStringText}`);
    return response.data;
  }

  async list(request: ListActivityDefinitionsRequest): Promise<PagedList<ActivityDefinitionSummary>> {
    const queryString: any = {};

    if (!!request.versionOptions) queryString.versionOptions = getVersionOptionsString(request.versionOptions);

    if (!!request.page) queryString.page = request.page;

    if (!!request.pageSize) queryString.pageSize = request.pageSize;

    if (!!request.pageSize) queryString.orderBy = request.orderBy;

    const queryStringText = serializeQueryString(queryString);
    const httpClient = await this.provider.getHttpClient();
    const response = await httpClient.get<PagedList<ActivityDefinitionSummary>>(`workflow-definitions${queryStringText}`);
    return response.data;
  }

  async export(request: ExportWorkflowRequest): Promise<ExportWorkflowResponse> {
    const queryString = {};

    if (!!request.versionOptions) queryString['versionOptions'] = getVersionOptionsString(request.versionOptions);

    const queryStringText = serializeQueryString(queryString);
    const definitionId = request.definitionId;

    const httpClient = await this.provider.getHttpClient();
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
    const httpClient = await this.provider.getHttpClient();

    const response = await httpClient.post<ActivityDefinition>(`workflow-definitions/${definitionId}/import`, json, {
      headers: {
        'Content-Type': 'application/json',
      },
    });

    const ActivityDefinition = response.data;
    return {ActivityDefinition: ActivityDefinition};
  }

  async deleteMany(request: DeleteManyActivityDefinitionRequest): Promise<DeleteManyActivityDefinitionResponse> {
    const httpClient = await this.provider.getHttpClient();

    const response = await httpClient.post<DeleteManyActivityDefinitionResponse>(`bulk-actions/delete/workflow-definitions/by-definition-id`, {
      definitionIds: request.definitionIds,
    });

    return response.data;
  }

  async publishMany(request: PublishManyActivityDefinitionRequest): Promise<PublishManyActivityDefinitionResponse> {
    const httpClient = await this.provider.getHttpClient();

    const response = await httpClient.post<PublishManyActivityDefinitionResponse>(`/bulk-actions/publish/workflow-definitions/by-definition-id`, {
      definitionIds: request.definitionIds,
    });

    return response.data;
  }

  async unpublishMany(request: UnpublishManyActivityDefinitionRequest): Promise<UnpublishManyActivityDefinitionResponse> {
    const httpClient = await this.provider.getHttpClient();

    const response = await httpClient.post<UnpublishManyActivityDefinitionResponse>(`/bulk-actions/retract/workflow-definitions/by-definition-id`, {
      definitionIds: request.definitionIds,
    });

    return response.data;
  }
}
