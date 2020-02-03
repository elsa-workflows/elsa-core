import { WorkflowPlugin } from "../models";
import { ActivityDefinition } from "../models";
export declare class ConsoleActivities implements WorkflowPlugin {
    private static readonly Category;
    getName: () => string;
    getActivityDefinitions: () => ActivityDefinition[];
    private readLine;
    private writeLine;
}
