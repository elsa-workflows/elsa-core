class FormUpdater {
    static updateEditor(activity, formData) {
        const newState = Object.assign({}, activity.state);
        formData.forEach((value, key) => {
            newState[key] = value;
        });
        return Object.assign({}, activity, { state: newState });
    }
}

class DefaultActivityHandler {
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

class ActivityManager {
    constructor() {
        this.addHandler = (activityTypeName, handler) => {
            this.handlers[activityTypeName] = Object.assign({}, handler);
        };
        this.renderDesigner = (activity, definition) => {
            const handler = this.getHandler(activity.type);
            if (!handler.renderDesigner)
                return {
                    title: activity.state.title || definition.displayName,
                    description: activity.state.description || definition.description,
                    icon: definition.icon || 'fas fa-cog'
                };
            return handler.renderDesigner(activity, definition);
        };
        this.updateEditor = (activity, formData) => {
            const handler = this.getHandler(activity.type);
            let updater = handler.updateEditor || FormUpdater.updateEditor;
            return updater(activity, formData);
        };
        this.getOutcomes = (activity, definition) => {
            const handler = this.getHandler(activity.type);
            if (!handler.getOutcomes)
                return [];
            return handler.getOutcomes(activity, definition);
        };
        this.getHandler = type => this.handlers[type] || new DefaultActivityHandler();
        this.handlers = {};
    }
}
const ActivityManager$1 = new ActivityManager();

export { ActivityManager$1 as A };
