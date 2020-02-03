export declare type WorkflowFormat = 'json' | 'yaml' | 'xml' | 'object';
export declare type WorkflowFormatDescriptor = {
    format: WorkflowFormat;
    fileExtension: string;
    mimeType: string;
    displayName: string;
};
export declare type WorkflowFormatDescriptorDictionary = {
    [key: string]: WorkflowFormatDescriptor;
};
