import {Activity, ActivityStats} from "../models";

export const activityTagName = "elsa-activity-wrapper";

export function createActivityElement(activity: Activity, detached?: boolean, selectedPort?: string, stats?: ActivityStats): HTMLElement {
    const activityElement = document.createElement(activityTagName) as any;
    const activityId = activity.id;
    const elementId = `activity-${activityId}`;

    if(!detached) {
        activityElement.id = elementId;
        activityElement.setAttribute("element-id", elementId);
    }
    
    if(!!selectedPort) 
        activityElement.setAttribute("selected-port-name", selectedPort);
    
    activityElement.stats = stats;
    //activityElement.activity = activity; // activity can be too deeply nested when dealing with workflow definition activities, exceeding the max depth of 32 with JSInterop.
    activityElement.setAttribute("activity-json", JSON.stringify(activity));
    activityElement.setAttribute("activity-id", activityId);
    
    return activityElement;
}