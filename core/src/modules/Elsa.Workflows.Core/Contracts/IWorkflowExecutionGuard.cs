namespace Elsa.Workflows;

/// <summary>Optional host authority boundary for workflows owned by a durable admission protocol.</summary>
public interface IWorkflowExecutionGuard
{
    /// <summary>Rejects owned identities before ordinary preparation or management effects. Uncertain ownership must fail closed.</summary>
    ValueTask DemandUnownedAsync(string instanceId, CancellationToken cancellationToken = default);

    /// <summary>Authorizes one exact prepared invocation, or returns null only for a definitively unowned workflow.</summary>
    ValueTask<IWorkflowExecutionAuthorization?> AuthorizeAsync(WorkflowExecutionContext context, WorkflowExecutionEntryPoint entryPoint);
}

/// <summary>The execution boundary requesting authorization.</summary>
public enum WorkflowExecutionEntryPoint
{
    /// <summary>The built-in runner before notifications and middleware.</summary>
    Runner,
    /// <summary>A public pipeline invocation, including previously built delegates. Never grants owned authority.</summary>
    DirectPipeline
}

/// <summary>An opaque runner-local revalidation ticket. No public execution API accepts this ticket.</summary>
public interface IWorkflowExecutionAuthorization
{
    /// <summary>Rejects changes to the exact prepared invocation immediately before middleware entry.</summary>
    ValueTask RevalidateAsync(CancellationToken cancellationToken = default);
}
