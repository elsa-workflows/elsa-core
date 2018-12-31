///<reference path="source-endpoint.ts"/>
///<reference path="target-endpoint.ts"/>
namespace Elsa {
    export interface IConnection {
        source: ISourceEndpoint;
        target: ITargetEndpoint;
    }
}