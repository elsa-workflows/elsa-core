import { r as registerInstance, h, H as Host } from './chunk-25ccd4a5.js';

class ListField {
    constructor(hostRef) {
        registerInstance(this, hostRef);
    }
    render() {
        const name = this.name;
        const label = this.label;
        const items = this.items;
        return (h(Host, null, h("label", { htmlFor: name }, label), h("input", { id: name, name: name, type: "text", class: "form-control", value: items }), h("small", { class: "form-text text-muted" }, this.hint)));
    }
    static get style() { return ""; }
}

export { ListField as wf_list_field };
