import { h } from "@stencil/core";
export class ImportExport {
    constructor() {
        this.importWorkflow = () => {
            const file = this.fileInput.files[0];
            const reader = new FileReader();
            reader.onload = async () => {
                const data = reader.result;
                const format = 'json';
                const importedData = {
                    data: data,
                    format: format
                };
                await this.import(importedData);
            };
            reader.readAsText(file);
        };
        this.serialize = (workflow, format) => {
            switch (format) {
                case 'json':
                    return JSON.stringify(workflow);
                case 'yaml':
                    return JSON.stringify(workflow);
                case 'xml':
                    return JSON.stringify(workflow);
                default:
                    return workflow;
            }
        };
    }
    async export(designer, formatDescriptor) {
        let blobUrl = this.blobUrl;
        if (!!blobUrl) {
            window.URL.revokeObjectURL(blobUrl);
        }
        const workflow = designer.workflow;
        const data = this.serialize(workflow, formatDescriptor.format);
        const blob = new Blob([data], { type: formatDescriptor.mimeType });
        this.blobUrl = blobUrl = window.URL.createObjectURL(blob);
        const downloadLink = document.createElement('a');
        downloadLink.setAttribute('href', blobUrl);
        downloadLink.setAttribute('download', `workflow.${formatDescriptor.fileExtension}`);
        document.body.appendChild(downloadLink);
        downloadLink.click();
        document.body.removeChild(downloadLink);
    }
    async import(data) {
        if (!data) {
            this.fileInput.click();
        }
        else {
            const workflow = JSON.parse(data.data);
            this.importEvent.emit(workflow);
        }
    }
    render() {
        return (h("host", null,
            h("input", { type: "file", class: "import-button", onChange: this.importWorkflow, ref: el => this.fileInput = el })));
    }
    static get is() { return "wf-import-export"; }
    static get originalStyleUrls() { return {
        "$": ["import-export.scss"]
    }; }
    static get styleUrls() { return {
        "$": ["import-export.css"]
    }; }
    static get events() { return [{
            "method": "importEvent",
            "name": "import-workflow",
            "bubbles": true,
            "cancelable": true,
            "composed": true,
            "docs": {
                "tags": [],
                "text": ""
            },
            "complexType": {
                "original": "Workflow",
                "resolved": "{ activities: Activity[]; connections: Connection[]; }",
                "references": {
                    "Workflow": {
                        "location": "import",
                        "path": "../../../models"
                    }
                }
            }
        }]; }
    static get methods() { return {
        "export": {
            "complexType": {
                "signature": "(designer: HTMLWfDesignerElement, formatDescriptor: WorkflowFormatDescriptor) => Promise<void>",
                "parameters": [{
                        "tags": [],
                        "text": ""
                    }, {
                        "tags": [],
                        "text": ""
                    }],
                "references": {
                    "Promise": {
                        "location": "global"
                    },
                    "HTMLWfDesignerElement": {
                        "location": "global"
                    },
                    "WorkflowFormatDescriptor": {
                        "location": "import",
                        "path": "../../../models"
                    }
                },
                "return": "Promise<void>"
            },
            "docs": {
                "text": "",
                "tags": []
            }
        },
        "import": {
            "complexType": {
                "signature": "(data?: ImportedWorkflowData) => Promise<void>",
                "parameters": [{
                        "tags": [],
                        "text": ""
                    }],
                "references": {
                    "Promise": {
                        "location": "global"
                    },
                    "ImportedWorkflowData": {
                        "location": "import",
                        "path": "../../../models"
                    },
                    "Workflow": {
                        "location": "import",
                        "path": "../../../models"
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
}
