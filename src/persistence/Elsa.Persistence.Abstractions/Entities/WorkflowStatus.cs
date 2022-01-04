namespace Elsa.Persistence.Entities;

public enum WorkflowStatus
{
    Idle,
    Running,
    Suspended,
    Finished,
    Compensating,
    Cancelled,
    Faulted,
}