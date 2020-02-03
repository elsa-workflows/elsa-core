import { WorkflowPlugin } from "../models";
import { ActivityDefinition } from "../models";
export declare class PrimitiveActivities implements WorkflowPlugin {
    private static readonly Category;
    getName: () => string;
    getActivityDefinitions: () => ActivityDefinition[];
    private log;
    private setVariable;
}
