import { WorkflowPlugin } from "../models";
import { ActivityDefinition } from "../models";
export declare class EmailActivities implements WorkflowPlugin {
    private static readonly Category;
    getName: () => string;
    getActivityDefinitions: () => ActivityDefinition[];
    private sendEmail;
}
