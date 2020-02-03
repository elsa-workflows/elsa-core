import { WorkflowPlugin } from "../models";
import { ActivityDefinition } from "../models";
export declare class TimerActivities implements WorkflowPlugin {
    private static readonly Category;
    getName: () => string;
    getActivityDefinitions: () => ActivityDefinition[];
    private timerEvent;
}
