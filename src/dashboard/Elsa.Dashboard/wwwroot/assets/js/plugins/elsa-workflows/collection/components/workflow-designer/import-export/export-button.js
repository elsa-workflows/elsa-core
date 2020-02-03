import { h } from "@stencil/core";
export class ExportButton {
    constructor() {
        this.workflowFormats = {
            json: {
                format: 'json',
                fileExtension: '.json',
                mimeType: 'application/json',
                displayName: 'JSON'
            },
            yaml: {
                format: 'yaml',
                fileExtension: '.yaml',
                mimeType: 'application/x-yaml',
                displayName: 'YAML'
            },
            xml: {
                format: 'xml',
                fileExtension: '.xml',
                mimeType: 'application/xml',
                displayName: 'XML'
            },
            object: {
                format: 'object',
                fileExtension: '.bin',
                mimeType: 'application/binary',
                displayName: 'Binary'
            }
        };
        this.getWorkflowHost = () => {
            return !!this.designerHostId ? document.querySelector(`#${this.designerHostId}`) : null;
        };
        this.handleExportClick = async (e, descriptor) => {
            e.preventDefault();
            this.exportClickedEvent.emit(descriptor);
            const host = this.getWorkflowHost();
            if (!!host) {
                await host.export(descriptor);
            }
        };
    }
    render() {
        const descriptors = this.workflowFormats;
        return (h("div", { class: "dropdown" },
            h("button", { class: "btn btn-secondary dropdown-toggle", type: "button", id: "exportButton", "data-toggle": "dropdown", "aria-haspopup": "true", "aria-expanded": "false" }, "Export"),
            h("div", { class: "dropdown-menu", "aria-labelledby": "exportButton" }, Object.keys(descriptors).map(key => {
                const descriptor = descriptors[key];
                return (h("a", { class: "dropdown-item", href: "#", onClick: e => this.handleExportClick(e, descriptor) }, descriptor.displayName));
            }))));
    }
    static get is() { return "wf-export-button"; }
    static get properties() { return {
        "designerHostId": {
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
            "attribute": "workflow-designer-host",
            "reflect": false
        },
        "workflowFormats": {
            "type": "unknown",
            "mutable": false,
            "complexType": {
                "original": "WorkflowFormatDescriptorDictionary",
                "resolved": "{ [key: string]: WorkflowFormatDescriptor; }",
                "references": {
                    "WorkflowFormatDescriptorDictionary": {
                        "location": "import",
                        "path": "../../../models"
                    }
                }
            },
            "required": false,
            "optional": false,
            "docs": {
                "tags": [],
                "text": ""
            },
            "defaultValue": "{\r\n    json: {\r\n      format: 'json',\r\n      fileExtension: '.json',\r\n      mimeType: 'application/json',\r\n      displayName: 'JSON'\r\n    },\r\n    yaml: {\r\n      format: 'yaml',\r\n      fileExtension: '.yaml',\r\n      mimeType: 'application/x-yaml',\r\n      displayName: 'YAML'\r\n    },\r\n    xml: {\r\n      format: 'xml',\r\n      fileExtension: '.xml',\r\n      mimeType: 'application/xml',\r\n      displayName: 'XML'\r\n    },\r\n    object: {\r\n      format: 'object',\r\n      fileExtension: '.bin',\r\n      mimeType: 'application/binary',\r\n      displayName: 'Binary'\r\n    }\r\n  }"
        }
    }; }
    static get events() { return [{
            "method": "exportClickedEvent",
            "name": "export",
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
    static get elementRef() { return "el"; }
}
