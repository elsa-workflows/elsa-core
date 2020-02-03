import { Activity, ActivityDefinition, RenderDesignerResult } from "../models";
import { ActivityHandler } from "./activity-handler";
export declare class ActivityManager {
    constructor();
    private readonly handlers;
    addHandler: (activityTypeName: string, handler: ActivityHandler) => void;
    renderDesigner: (activity: Activity, definition: ActivityDefinition) => RenderDesignerResult;
    updateEditor: (activity: Activity, formData: FormData) => Activity;
    getOutcomes: (activity: Activity, definition: ActivityDefinition) => string[];
    private getHandler;
}
declare const _default: ActivityManager;
export default _default;
