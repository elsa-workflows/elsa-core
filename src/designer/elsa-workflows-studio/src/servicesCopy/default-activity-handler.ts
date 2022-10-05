import { ActivityHandler } from "./activity-handler";
import { Activity, ActivityDefinitionCopy, RenderDesignerResult } from "../modelsCopy";
import { FormUpdater } from "../utils";

export class DefaultActivityHandler implements ActivityHandler {
  renderDesigner = (activity: Activity, definition: ActivityDefinitionCopy): RenderDesignerResult => {
    let description = null;

    if (activity.state.description)
      description = activity.state.description;
    else if (!!definition.runtimeDescription)
      description = definition.runtimeDescription;
    else
      description = definition.description;

    try {
      const fun = eval(description);

      description = fun({ activity, definition, state: activity.state });
    } catch {
    }

    return {
      title: activity.state.title || definition.displayName,
      description: description,
      icon: definition.icon || 'fas fa-cog'
    }
  };

  updateEditor = (activity: Activity, formData: FormData): Activity => FormUpdater.updateEditor(activity, formData);

  getOutcomes = (activity: Activity, definition: ActivityDefinitionCopy): Array<string> => {
    let outcomes = [];

    if (!!definition) {
      const lambda = definition.outcomes;

      if (lambda instanceof Array) {
        outcomes = lambda as Array<string>;
      } else {
        const value = eval(lambda);

        if (value instanceof Array)
          outcomes = value;

        else if (value instanceof Function) {
          try {
            outcomes = value({ activity, definition, state: activity.state });
          } catch (e) {
            console.warn(e);
            outcomes = [];
          }
        }
      }
    }

    return !!outcomes ? outcomes : [];
  }
}
