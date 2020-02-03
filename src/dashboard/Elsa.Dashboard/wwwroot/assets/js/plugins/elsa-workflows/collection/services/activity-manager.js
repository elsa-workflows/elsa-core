import { FormUpdater } from "../utils";
import { DefaultActivityHandler } from "./default-activity-handler";
export class ActivityManager {
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
export default new ActivityManager();
