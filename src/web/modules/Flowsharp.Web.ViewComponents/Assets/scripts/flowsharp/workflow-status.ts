namespace Flowsharp {
    export enum WorkflowStatus {
        Idle,
        Starting,
        Resuming,
        Executing,
        Halted,
        Finished,
        Faulted,
        Aborted
    }
}