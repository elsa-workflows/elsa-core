import { Activity, ActivityDefinitionCopy, ActivityHandlerMap, RenderDesignerResult } from "../modelsCopy";
import { ActivityHandler } from "./activity-handler";
import { FormUpdater } from "../utils";
import { DefaultActivityHandler } from "./default-activity-handler";

export class ActivityManager {
  constructor(){
    this.handlers= {};
  }

  private readonly handlers: ActivityHandlerMap;

  addHandler = (activityTypeName: string, handler: ActivityHandler) => {
    this.handlers[activityTypeName] = {...handler};
  };

  renderDesigner = (activity: Activity, definition?: ActivityDefinitionCopy): RenderDesignerResult => {
    const handler = this.getHandler(activity.type);

    if(!handler.renderDesigner)
      return {
        title: activity.state.title || definition.displayName,
        description: activity.state.description || definition.description,
        icon: definition.icon || 'fas fa-cog'
      };

    if(!definition)
      return {
        title: activity.state.title || "!!",
        description: activity.state.description || "no description",
        icon: 'fas fa-cog'
      };


    return handler.renderDesigner(activity, definition);
  };

  updateEditor = (activity: Activity, formData: FormData): Activity => {
    const handler = this.getHandler(activity.type);
    let updater = handler.updateEditor  || FormUpdater.updateEditor;

    return updater(activity, formData);
  };

  getOutcomes = (activity: Activity, definition: ActivityDefinitionCopy): Array<string> => {
    const handler = this.getHandler(activity.type);

    if(!handler.getOutcomes)
      return [];

    return handler.getOutcomes(activity, definition);
  };

  private getHandler = type => this.handlers[type] || new DefaultActivityHandler();
}

export default new ActivityManager();
