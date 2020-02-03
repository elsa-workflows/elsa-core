import { WorkflowPlugin } from "../models";
import { ActivityDefinition } from "../models";
export declare class HttpActivities implements WorkflowPlugin {
    private static readonly Category;
    getName: () => string;
    getActivityDefinitions: () => ActivityDefinition[];
    private sendHttpRequest;
    private handleHttpRequest;
    private sendHttpResponse;
}
