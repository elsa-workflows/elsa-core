import {ActivityDesignerMetadata, ActivityDisplayTextMetadata} from "./metadata";

export interface Activity {
    id: string;
    type: string;
    version: number;
    metadata: any | ActivityDisplayTextMetadata | ActivityDesignerMetadata;
    canStartWorkflow?: boolean;
    runAsynchronously?: boolean;
    customProperties?: any;

    [name: string]: any;
}

