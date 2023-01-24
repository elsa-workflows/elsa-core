import {Service} from "typedi";
import {
  ActivityDefinition,
  ActivityDefinitionSummary,
  DeleteActivityDefinitionRequest, DeleteManyActivityDefinitionRequest, DeleteManyActivityDefinitionResponse, ExportActivityDefinitionRequest, ExportActivityDefinitionResponse,
  GetActivityDefinitionRequest, ImportActivityDefinitionRequest, ImportActivityDefinitionResponse,
  ListActivityDefinitionsRequest,
  PublishActivityDefinitionRequest, PublishManyActivityDefinitionRequest, PublishManyActivityDefinitionResponse,
  RetractActivityDefinitionRequest,
  SaveActivityDefinitionRequest, UnpublishManyActivityDefinitionRequest, UnpublishManyActivityDefinitionResponse,
  DeleteActivityVersionRequest, RevertActivityVersionRequest
} from "../models";
import {getVersionOptionsString, serializeQueryString} from "../../../utils";
import {PagedList} from "../../../models";
import {ElsaClientProvider} from "../../../services";

@Service()
export class ActivityDefinitionsApi {
  private provider: ElsaClientProvider;

  constructor(provider: ElsaClientProvider) {
    this.provider = provider;
  }

  async publish(request: PublishActivityDefinitionRequest): Promise<ActivityDefinition> {
    const httpClient = await this.provider.getHttpClient();
    const response = await httpClient.post<ActivityDefinition>(`activity-definitions/${request.definitionId}/publish`);
    return response.data;
  }

  async retract(request: RetractActivityDefinitionRequest): Promise<ActivityDefinition> {
    const httpClient = await this.provider.getHttpClient();
    const response = await httpClient.post<ActivityDefinition>(`activity-definitions/${request.definitionId}/retract`);
    return response.data;
  }

  async delete(request: DeleteActivityDefinitionRequest): Promise<ActivityDefinition> {
    const httpClient = await this.provider.getHttpClient();
    const response = await httpClient.delete<ActivityDefinition>(`activity-definitions/${request.definitionId}`);
    return response.data;
  }

  async post(request: SaveActivityDefinitionRequest): Promise<ActivityDefinition> {
    const httpClient = await this.provider.getHttpClient();
    const response = await httpClient.post<ActivityDefinition>('activity-definitions', request);
    return response.data;
  }

  async get(request: GetActivityDefinitionRequest): Promise<ActivityDefinition> {
    const queryString = {};

    if (!!request.versionOptions) queryString['versionOptions'] = getVersionOptionsString(request.versionOptions);

    const queryStringText = serializeQueryString(queryString);
    const httpClient = await this.provider.getHttpClient();
    const response = await httpClient.get<ActivityDefinition>(`activity-definitions/${request.definitionId}${queryStringText}`);
    return response.data;
  }

  async getVersions(activityDefinitionId: string): Promise<Array<ActivityDefinition>> {
    const httpClient = await this.provider.getHttpClient();
    const response = await httpClient.get<Array<ActivityDefinition>>(`activity-definitions/${activityDefinitionId}/versions`);
    return response.data;
  }

  async deleteVersion(request: DeleteActivityVersionRequest): Promise<ActivityDefinition> {
    const httpClient = await this.provider.getHttpClient();
    const response = await httpClient.delete<ActivityDefinition>(`activity-definitions/${request.definitionId}/version/${request.version}`);
    return response.data;
  }

  async revertVersion(request: RevertActivityVersionRequest): Promise<ActivityDefinition> {
    const httpClient = await this.provider.getHttpClient();
    const response = await httpClient.post<ActivityDefinition>(`activity-definitions/${request.definitionId}/revert/${request.version}`);
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
    const response = await httpClient.get<PagedList<ActivityDefinitionSummary>>(`activity-definitions${queryStringText}`);
    return response.data;
  }

  async export(request: ExportActivityDefinitionRequest): Promise<ExportActivityDefinitionResponse> {
    const queryString = {};

    if (!!request.versionOptions) queryString['versionOptions'] = getVersionOptionsString(request.versionOptions);

    const queryStringText = serializeQueryString(queryString);
    const definitionId = request.definitionId;

    const httpClient = await this.provider.getHttpClient();
    const response = await httpClient.get(`activity-definitions/${request.definitionId}/export${queryStringText}`, {
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

  async import(request: ImportActivityDefinitionRequest): Promise<ImportActivityDefinitionResponse> {
    const file = request.file;
    const definitionId = request.definitionId;
    const json = await file.text();
    const httpClient = await this.provider.getHttpClient();

    const response = await httpClient.post<ActivityDefinition>(`activity-definitions/${definitionId}/import`, json, {
      headers: {
        'Content-Type': 'application/json',
      },
    });

    const activityDefinition = response.data;
    return {activityDefinition: activityDefinition};
  }

  async deleteMany(request: DeleteManyActivityDefinitionRequest): Promise<DeleteManyActivityDefinitionResponse> {
    const httpClient = await this.provider.getHttpClient();

    const response = await httpClient.post<DeleteManyActivityDefinitionResponse>(`bulk-actions/delete/activity-definitions/by-definition-id`, {
      definitionIds: request.definitionIds,
    });

    return response.data;
  }

  async publishMany(request: PublishManyActivityDefinitionRequest): Promise<PublishManyActivityDefinitionResponse> {
    const httpClient = await this.provider.getHttpClient();

    const response = await httpClient.post<PublishManyActivityDefinitionResponse>(`/bulk-actions/publish/activity-definitions/by-definition-id`, {
      definitionIds: request.definitionIds,
    });

    return response.data;
  }

  async unpublishMany(request: UnpublishManyActivityDefinitionRequest): Promise<UnpublishManyActivityDefinitionResponse> {
    const httpClient = await this.provider.getHttpClient();

    const response = await httpClient.post<UnpublishManyActivityDefinitionResponse>(`/bulk-actions/retract/activity-definitions/by-definition-id`, {
      definitionIds: request.definitionIds,
    });

    return response.data;
  }
}