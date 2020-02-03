import { r as registerInstance, h, H as Host } from './chunk-25ccd4a5.js';
import { D as DisplayManager } from './chunk-80cbdbf5.js';
import { A as ActivityDisplayMode } from './chunk-4e91c3fe.js';
import { A as ActivityManager } from './chunk-490e3781.js';

class ActivityRenderer {
    constructor(hostRef) {
        registerInstance(this, hostRef);
        this.displayMode = ActivityDisplayMode.Design;
    }
    render() {
        if (!this.activity || !this.activityDefinition)
            return null;
        switch (this.displayMode) {
            case ActivityDisplayMode.Design:
                return this.renderDesigner();
            case ActivityDisplayMode.Edit:
                return this.renderEditor();
        }
    }
    renderDesigner() {
        const activity = this.activity;
        const definition = this.activityDefinition;
        const result = ActivityManager.renderDesigner(activity, definition);
        const iconClass = `${result.icon} mr-1`;
        return (h("div", null, h("h5", null, h("i", { class: iconClass }), result.title), h("p", { innerHTML: result.description })));
    }
    renderEditor() {
        const activity = this.activity;
        const definition = this.activityDefinition;
        const properties = definition.properties;
        return (h(Host, null, properties.map(property => {
            const html = DisplayManager.displayEditor(activity, property);
            return h("div", { class: "form-group", innerHTML: html });
        })));
    }
    async updateEditor(formData) {
        const activity = Object.assign({}, this.activity);
        const definition = this.activityDefinition;
        const properties = definition.properties;
        for (const property of properties) {
            DisplayManager.updateEditor(activity, property, formData);
        }
        return activity;
    }
    static get style() { return ""; }
}

export { ActivityRenderer as wf_activity_renderer };
