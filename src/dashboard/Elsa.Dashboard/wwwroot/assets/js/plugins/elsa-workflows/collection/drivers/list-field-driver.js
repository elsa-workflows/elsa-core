export class ListFieldDriver {
    constructor() {
        this.displayEditor = (activity, property) => {
            const name = property.name;
            const label = property.label;
            const items = activity.state[name] || [];
            const value = items.join(', ');
            return `<wf-list-field name="${name}" label="${label}" hint="${property.hint}" items="${value}"></wf-list-field>`;
        };
        this.updateEditor = (activity, property, formData) => {
            const value = formData.get(property.name).toString();
            activity.state[property.name] = value.split(',').map(x => x.trim());
        };
    }
}
