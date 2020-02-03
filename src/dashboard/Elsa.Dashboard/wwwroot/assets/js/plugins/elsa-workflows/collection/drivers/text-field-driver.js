export class TextFieldDriver {
    constructor() {
        this.displayEditor = (activity, property) => {
            const name = property.name;
            const label = property.label;
            const value = activity.state[name] || '';
            return `<wf-text-field name="${name}" label="${label}" hint="${property.hint}" value="${value}"></wf-text-field>`;
        };
        this.updateEditor = (activity, property, formData) => {
            activity.state[property.name] = formData.get(property.name).toString().trim();
        };
    }
}
