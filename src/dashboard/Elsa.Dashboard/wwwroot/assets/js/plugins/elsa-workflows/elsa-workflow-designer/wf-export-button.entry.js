import { r as registerInstance, c as createEvent, h, d as getElement } from './chunk-25ccd4a5.js';

class ExportButton {
    constructor(hostRef) {
        registerInstance(this, hostRef);
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
        this.exportClickedEvent = createEvent(this, "export", 7);
    }
    render() {
        const descriptors = this.workflowFormats;
        return (h("div", { class: "dropdown" }, h("button", { class: "btn btn-secondary dropdown-toggle", type: "button", id: "exportButton", "data-toggle": "dropdown", "aria-haspopup": "true", "aria-expanded": "false" }, "Export"), h("div", { class: "dropdown-menu", "aria-labelledby": "exportButton" }, Object.keys(descriptors).map(key => {
            const descriptor = descriptors[key];
            return (h("a", { class: "dropdown-item", href: "#", onClick: e => this.handleExportClick(e, descriptor) }, descriptor.displayName));
        }))));
    }
    get el() { return getElement(this); }
}

export { ExportButton as wf_export_button };
