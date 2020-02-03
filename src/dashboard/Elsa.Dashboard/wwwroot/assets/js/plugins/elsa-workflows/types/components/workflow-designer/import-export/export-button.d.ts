import { EventEmitter } from '../../../stencil.core';
import { WorkflowFormatDescriptorDictionary } from "../../../models";
export declare class ExportButton {
    el: HTMLElement;
    exportClickedEvent: EventEmitter;
    designerHostId: string;
    workflowFormats: WorkflowFormatDescriptorDictionary;
    render(): any;
    private getWorkflowHost;
    private handleExportClick;
}
