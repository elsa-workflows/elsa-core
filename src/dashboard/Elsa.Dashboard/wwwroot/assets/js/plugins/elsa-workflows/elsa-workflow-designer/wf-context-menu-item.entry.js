import { r as registerInstance, h, d as getElement } from './chunk-25ccd4a5.js';

class ContextMenuItem {
    constructor(hostRef) {
        registerInstance(this, hostRef);
    }
    render() {
        const text = this.text;
        return (h("a", { class: "dropdown-item", href: "#", onClick: e => e.preventDefault() }, text));
    }
    get el() { return getElement(this); }
    static get style() { return ""; }
}

export { ContextMenuItem as wf_context_menu_item };
