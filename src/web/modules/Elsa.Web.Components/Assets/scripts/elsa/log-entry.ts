///<reference path="source-endpoint.ts"/>
///<reference path="target-endpoint.ts"/>
namespace Elsa {
    export interface LogEntry {
        activityId: string;
        timestamp: Date;
        faulted: boolean;
        message: string;
    }
}