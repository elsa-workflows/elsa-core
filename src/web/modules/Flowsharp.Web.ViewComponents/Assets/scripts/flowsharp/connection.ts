///<reference path="source-endpoint.ts"/>
///<reference path="target-endpoint.ts"/>
namespace Flowsharp {
    export interface IConnection {
        source: ISourceEndpoint;
        target: ITargetEndpoint;
    }
}