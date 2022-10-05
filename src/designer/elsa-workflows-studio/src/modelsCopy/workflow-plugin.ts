import { ActivityDefinitionCopy } from "./activity-definition";

export interface WorkflowPlugin {
  getName: () => string;
  getActivityDefinitions: () => Array<ActivityDefinitionCopy>
}
