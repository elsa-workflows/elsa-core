import { r as registerInstance, h, H as Host, d as getElement } from './chunk-25ccd4a5.js';

class SelectField {
    constructor(hostRef) {
        registerInstance(this, hostRef);
        this.renderItem = (item) => {
            const isGroup = !!item.options;
            return isGroup ? this.renderGroup(item) : this.renderOption(item);
        };
        this.renderOption = (option) => {
            const type = typeof (option);
            let label = null;
            let value = null;
            switch (type) {
                case 'string':
                    label = option;
                    value = option;
                    break;
                case 'number':
                    label = option.toString();
                    value = option.toString();
                    break;
                case 'object':
                    const pair = option;
                    label = pair.label;
                    value = pair.value;
                    break;
                default:
                    throw Error(`Unsupported option type: ${type}.`);
            }
            const isSelected = value === this.value;
            return h("option", { value: value, selected: isSelected }, label);
        };
        this.renderGroup = (group) => {
            return (h("optgroup", { label: group.label }, group.options.map(this.renderOption)));
        };
    }
    componentWillLoad() {
        const encodedJson = this.element.getAttribute('data-items');
        if (!encodedJson)
            return;
        const json = decodeURI(encodedJson);
        this.items = JSON.parse(json);
    }
    render() {
        const name = this.name;
        const label = this.label;
        const items = this.items || [];
        return (h(Host, null, h("label", { htmlFor: name }, label), h("select", { id: name, name: name, class: "custom-select" }, items.map(this.renderItem)), h("small", { class: "form-text text-muted" }, this.hint)));
    }
    get element() { return getElement(this); }
    static get style() { return ""; }
}

export { SelectField as wf_select_field };
