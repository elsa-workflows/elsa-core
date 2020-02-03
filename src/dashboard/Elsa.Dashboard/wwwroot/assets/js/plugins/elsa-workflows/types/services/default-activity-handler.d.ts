import { ActivityHandler } from "./activity-handler";
import { Activity, ActivityDefinition, RenderDesignerResult } from "../models";
export declare class DefaultActivityHandler implements ActivityHandler {
    renderDesigner: (activity: Activity, definition: ActivityDefinition) => RenderDesignerResult;
    updateEditor: (activity: Activity, formData: FormData) => Activity;
    getOutcomes: (activity: Activity, definition: ActivityDefinition) => string[];
}
