import { h, Host } from "@stencil/core";
export class ContextMenu {
    constructor() {
        this.isHidden = true;
        this.position = { left: 0, top: 0 };
        this.onContextMenu = (e) => {
            if (e.defaultPrevented)
                return;
            e.preventDefault();
            this.contextMenuEvent.emit();
            this.position = { left: e.pageX, top: e.pageY };
            this.isHidden = false;
        };
        this.onContextMenuClick = () => {
            this.isHidden = true;
        };
    }
    targetChangeHandler(newValue, oldValue) {
        if (!!oldValue) {
            oldValue.removeEventListener('contextmenu', this.onContextMenu);
        }
        this.setupTarget(newValue);
    }
    targetSelectorChangeHandler(newValue) {
        this.target = document.querySelector(newValue);
    }
    handleBodyClick() {
        this.isHidden = true;
    }
    handleContextMenu() {
        this.isHidden = true;
    }
    async handleContextMenuEvent(e) {
        this.onContextMenu(e);
    }
    componentDidLoad() {
        this.setupTarget(this.target);
    }
    render() {
        const css = {
            left: `${this.position.left}px`,
            top: `${this.position.top}px`,
            display: this.isHidden ? 'none' : 'block'
        };
        return (h(Host, { class: "dropdown-menu context-menu canvas-context-menu position-fixed", style: css, onClick: this.onContextMenuClick },
            h("slot", null)));
    }
    setupTarget(value) {
        if (!!value) {
            value.addEventListener('contextmenu', this.onContextMenu, { capture: false });
        }
    }
    static get is() { return "wf-context-menu"; }
    static get originalStyleUrls() { return {
        "$": ["context-menu.scss"]
    }; }
    static get styleUrls() { return {
        "$": ["context-menu.css"]
    }; }
    static get properties() { return {
        "target": {
            "type": "unknown",
            "mutable": false,
            "complexType": {
                "original": "HTMLElement | ShadowRoot",
                "resolved": "HTMLElement | ShadowRoot",
                "references": {
                    "HTMLElement": {
                        "location": "global"
                    },
                    "ShadowRoot": {
                        "location": "global"
                    }
                }
            },
            "required": false,
            "optional": false,
            "docs": {
                "tags": [],
                "text": ""
            }
        },
        "targetSelector": {
            "type": "string",
            "mutable": false,
            "complexType": {
                "original": "string",
                "resolved": "string",
                "references": {}
            },
            "required": false,
            "optional": false,
            "docs": {
                "tags": [],
                "text": ""
            },
            "attribute": "target",
            "reflect": true
        }
    }; }
    static get states() { return {
        "isHidden": {},
        "position": {}
    }; }
    static get events() { return [{
            "method": "contextMenuEvent",
            "name": "context-menu",
            "bubbles": true,
            "cancelable": true,
            "composed": true,
            "docs": {
                "tags": [],
                "text": ""
            },
            "complexType": {
                "original": "any",
                "resolved": "any",
                "references": {}
            }
        }]; }
    static get methods() { return {
        "handleContextMenuEvent": {
            "complexType": {
                "signature": "(e: MouseEvent) => Promise<void>",
                "parameters": [{
                        "tags": [],
                        "text": ""
                    }],
                "references": {
                    "Promise": {
                        "location": "global"
                    },
                    "MouseEvent": {
                        "location": "global"
                    }
                },
                "return": "Promise<void>"
            },
            "docs": {
                "text": "",
                "tags": []
            }
        }
    }; }
    static get elementRef() { return "el"; }
    static get watchers() { return [{
            "propName": "target",
            "methodName": "targetChangeHandler"
        }, {
            "propName": "targetSelector",
            "methodName": "targetSelectorChangeHandler"
        }]; }
    static get listeners() { return [{
            "name": "click",
            "method": "handleBodyClick",
            "target": "body",
            "capture": false,
            "passive": false
        }, {
            "name": "context-menu",
            "method": "handleContextMenu",
            "target": "body",
            "capture": false,
            "passive": false
        }]; }
}
