import { r as registerInstance, c as createEvent, h, d as getElement } from './chunk-25ccd4a5.js';

class ImportExport {
    constructor(hostRef) {
        registerInstance(this, hostRef);
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
        this.importEvent = createEvent(this, "import-workflow", 7);
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
        return (h("host", null, h("input", { type: "file", class: "import-button", onChange: this.importWorkflow, ref: el => this.fileInput = el })));
    }
    get el() { return getElement(this); }
    static get style() { return ".import-button {\n  display: none;\n}"; }
}

export { ImportExport as wf_import_export };
