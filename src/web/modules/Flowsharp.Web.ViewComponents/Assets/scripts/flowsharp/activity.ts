///<reference path="activity-metadata.ts"/>
///<reference path="endpoint.ts"/>

namespace Flowsharp {
    export interface IActivity {
        id: string;
        endpoints: IEndpoint[];
        metadata: IActivityMetadata;
    }   
}