using Elsa.Mediator.Contracts;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// Buffers notifications produced during workflow commit persistence until the commit succeeds.
/// </summary>
public interface IWorkflowCommitNotificationBuffer
{
    /// <summary>
    /// Begins buffering notifications on the current async flow.
    /// </summary>
    IWorkflowCommitNotificationScope Begin();
}

/// <summary>
/// Represents an active workflow commit notification buffer.
/// </summary>
public interface IWorkflowCommitNotificationScope : IDisposable
{
    /// <summary>
    /// Flushes buffered notifications.
    /// </summary>
    Task FlushAsync(CancellationToken cancellationToken = default);
}
