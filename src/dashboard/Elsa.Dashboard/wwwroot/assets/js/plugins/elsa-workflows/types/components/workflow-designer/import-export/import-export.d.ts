import { EventEmitter } from '../../../stencil.core';
import { ImportedWorkflowData, Workflow, WorkflowFormatDescriptor } from "../../../models";
export declare class ImportExport {
    blobUrl: string;
    fileInput: HTMLInputElement;
    el: HTMLElement;
    importEvent: EventEmitter<Workflow>;
    export(designer: HTMLWfDesignerElement, formatDescriptor: WorkflowFormatDescriptor): Promise<void>;
    import(data?: ImportedWorkflowData): Promise<void>;
    render(): any;
    private importWorkflow;
    private serialize;
}
