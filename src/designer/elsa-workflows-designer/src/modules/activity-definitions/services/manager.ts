import {Container, Service} from "typedi";
import {ActivityDefinitionsApi} from "./api";
import {ActivityDefinition, RetractActivityDefinitionRequest, SaveActivityDefinitionRequest} from "../models";

@Service()
export class ActivityDefinitionManager {
  private readonly api: ActivityDefinitionsApi;

  constructor() {
    this.api = Container.get(ActivityDefinitionsApi);
  }

  public save = async (definition: ActivityDefinition, publish: boolean): Promise<ActivityDefinition> => {
    const request: SaveActivityDefinitionRequest = {
      definitionId: definition.definitionId,
      name: definition.name,
      description: definition.description,
      publish: publish,
      root: definition.root,
      variables: definition.variables
    };

    return await this.api.post(request);
  }

  public retract = async (definition: ActivityDefinition): Promise<ActivityDefinition> => {
    const request: RetractActivityDefinitionRequest = {
      definitionId: definition.definitionId
    };

    return await this.api.retract(request);
  }
}
