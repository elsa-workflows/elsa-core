export class BooleanFieldDriver {
    constructor() {
        this.displayEditor = (activity, property) => {
            const name = property.name;
            const label = property.label;
            const checked = activity.state[name] === 'true';
            return `<wf-boolean-field name="${name}" label="${label}" hint="${property.hint}" checked="${checked}"></wf-boolean-field>`;
        };
        this.updateEditor = (activity, property, formData) => {
            activity.state[property.name] = formData.get(property.name);
        };
    }
}
