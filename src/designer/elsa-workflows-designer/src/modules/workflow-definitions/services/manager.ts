import {Container, Service} from "typedi";
import {WorkflowDefinition} from "../models/entities";
import {ExportWorkflowRequest, ImportWorkflowRequest, RetractWorkflowDefinitionRequest, SaveWorkflowDefinitionRequest, WorkflowDefinitionsApi} from "./api";
import {downloadFromBlob} from "../../../utils";

@Service()
export class WorkflowDefinitionManager {
  private readonly api: WorkflowDefinitionsApi;

  constructor() {
    this.api = Container.get(WorkflowDefinitionsApi);
  }

  public saveWorkflow = async (definition: WorkflowDefinition, publish: boolean): Promise<WorkflowDefinition> => {
    const request: SaveWorkflowDefinitionRequest = {
      definitionId: definition.definitionId,
      version: definition.version,
      name: definition.name,
      description: definition.description,
      publish: publish,
      root: definition.root,
      variables: definition.variables
    };

    return await this.api.post(request);
  };

  public retractWorkflow = async (definition: WorkflowDefinition): Promise<WorkflowDefinition> => {
    const request: RetractWorkflowDefinitionRequest = {
      definitionId: definition.definitionId
    };

    return await this.api.retract(request);
  };

  public exportWorkflow = async (definition: WorkflowDefinition): Promise<void> => {
    const request: ExportWorkflowRequest = {
      definitionId: definition.definitionId,
      versionOptions: {version: definition.version}
    };

    const response = await this.api.export(request);
    downloadFromBlob(response.data, {contentType: 'application/json', fileName: response.fileName});
  };

  public importWorkflow = async (definitionId: string, file: File): Promise<WorkflowDefinition> => {
    try {
      const importRequest: ImportWorkflowRequest = {definitionId, file};
      const importResponse = await this.api.import(importRequest);
      return importResponse.workflowDefinition;
    } catch (e) {
      console.error(e);
    }
  };

}
