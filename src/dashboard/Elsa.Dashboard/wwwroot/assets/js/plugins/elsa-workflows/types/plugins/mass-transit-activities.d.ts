import { WorkflowPlugin } from "../models";
import { ActivityDefinition } from "../models";
export declare class MassTransitActivities implements WorkflowPlugin {
    private static readonly Category;
    getName: () => string;
    getActivityDefinitions: () => ActivityDefinition[];
    private receiveMassTransitMessage;
    private sendMassTransitMessage;
}
