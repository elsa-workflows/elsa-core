///<reference path="activity-metadata.ts"/>

namespace Flowsharp {
    export interface IActivity {
        id: string;
        metadata: IActivityMetadata;
    }   
}