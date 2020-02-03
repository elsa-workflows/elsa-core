import { ActivityDefinition } from "./activity-definition";
export interface WorkflowPlugin {
    getName: () => string;
    getActivityDefinitions: () => Array<ActivityDefinition>;
}
