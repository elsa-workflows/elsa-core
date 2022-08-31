import {Container, Service} from "typedi";
import {WorkflowDefinition} from "../models/entities";
import {RetractWorkflowDefinitionRequest, SaveWorkflowDefinitionRequest, WorkflowDefinitionsApi} from "./api";

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
  }

  public retractWorkflow = async (workflow: WorkflowDefinition): Promise<WorkflowDefinition> => {
    const request: RetractWorkflowDefinitionRequest = {
      definitionId: workflow.definitionId
    };

    return await this.api.retract(request);
  }
}
