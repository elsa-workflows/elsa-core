import {Service} from "typedi";
import {WorkflowDefinition} from "../models";
import {RetractWorkflowDefinitionRequest, SaveWorkflowDefinitionRequest} from "./api-client/workflow-definitions-api";
import {ElsaApiClientProvider} from "./api-client/api-client";

@Service()
export class WorkflowDefinitionManager {
  private elsaApiClientProvider: ElsaApiClientProvider;

  constructor(elsaApiClientProvider: ElsaApiClientProvider) {
    this.elsaApiClientProvider = elsaApiClientProvider;
  }

  public saveWorkflow = async (definition: WorkflowDefinition, publish: boolean): Promise<WorkflowDefinition> => {
    const request: SaveWorkflowDefinitionRequest = {
      definitionId: definition.definitionId,
      name: definition.name,
      description: definition.description,
      publish: publish,
      root: definition.root,
      variables: definition.variables
    };

    const elsaClient = await this.elsaApiClientProvider.getElsaClient();
    return await elsaClient.workflowDefinitions.post(request);
  }

  public retractWorkflow = async (workflow: WorkflowDefinition): Promise<WorkflowDefinition> => {
    const request: RetractWorkflowDefinitionRequest = {
      definitionId: workflow.definitionId
    };

    const elsaClient = await this.elsaApiClientProvider.getElsaClient();
    return await elsaClient.workflowDefinitions.retract(request);
  }
}
