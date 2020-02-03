import { Activity, ActivityDefinition, ActivityDisplayMode } from "../../../models";
export declare class ActivityRenderer {
    activityDefinition: ActivityDefinition;
    activity: Activity;
    displayMode: ActivityDisplayMode;
    render(): any;
    renderDesigner(): any;
    renderEditor(): any;
    updateEditor(formData: FormData): Promise<Activity>;
}
