import { FormUpdater } from "../utils";
export class DefaultActivityHandler {
    constructor() {
        this.renderDesigner = (activity, definition) => {
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
            }
            catch (_a) {
            }
            return {
                title: activity.state.title || definition.displayName,
                description: description,
                icon: definition.icon || 'fas fa-cog'
            };
        };
        this.updateEditor = (activity, formData) => FormUpdater.updateEditor(activity, formData);
        this.getOutcomes = (activity, definition) => {
            let outcomes = [];
            if (!!definition) {
                const lambda = definition.outcomes;
                if (lambda instanceof Array) {
                    outcomes = lambda;
                }
                else {
                    const value = eval(lambda);
                    if (value instanceof Array)
                        outcomes = value;
                    else if (value instanceof Function) {
                        try {
                            outcomes = value({ activity, definition, state: activity.state });
                        }
                        catch (e) {
                            console.warn(e);
                            outcomes = [];
                        }
                    }
                }
            }
            return !!outcomes ? outcomes : [];
        };
    }
}
