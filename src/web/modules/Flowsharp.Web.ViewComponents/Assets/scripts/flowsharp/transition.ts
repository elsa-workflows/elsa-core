namespace Flowsharp {
    export interface ITransition {
        sourceActivityId: string;
        sourceOutcomeName: string;
        destinationActivityId: null;
    }
}