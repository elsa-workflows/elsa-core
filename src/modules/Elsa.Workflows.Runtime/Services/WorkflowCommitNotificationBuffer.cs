using Elsa.Mediator.Contracts;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Runtime;

/// <inheritdoc />
public class WorkflowCommitNotificationBuffer(IMediator mediator, ILogger<WorkflowCommitNotificationBuffer> logger) : IWorkflowCommitNotificationBuffer
{
    private readonly IMediator _mediator = mediator;
    private readonly ILogger<WorkflowCommitNotificationBuffer> _logger = logger;
    private readonly AsyncLocal<Scope?> _currentScope = new();

    /// <inheritdoc />
    public IWorkflowCommitNotificationScope Begin()
    {
        var scope = new Scope(this, _currentScope.Value);
        _currentScope.Value = scope;
        return scope;
    }

    internal bool TryAdd(INotification notification, IEventPublishingStrategy? strategy)
    {
        var scope = _currentScope.Value;
        if (scope == null)
            return false;

        scope.Add(notification, strategy);
        return true;
    }

    private class Scope(WorkflowCommitNotificationBuffer owner, Scope? parent) : IWorkflowCommitNotificationScope
    {
        private readonly List<Entry> _entries = [];
        private bool _disposed;

        public void Add(INotification notification, IEventPublishingStrategy? strategy)
        {
            _entries.Add(new(notification, strategy));
        }

        public async Task FlushAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            owner._currentScope.Value = parent;
            List<Exception>? exceptions = null;

            foreach (var entry in _entries)
            {
                try
                {
                    await owner._mediator.SendAsync(entry.Notification, entry.Strategy, cancellationToken);
                }
                catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
                {
                    owner._logger.LogError(ex, "Failed to publish buffered workflow commit notification {NotificationType}", entry.Notification.GetType().FullName);
                    exceptions ??= [];
                    exceptions.Add(ex);
                }
            }

            _entries.Clear();

            if (exceptions is { Count: > 0 })
                throw new AggregateException("One or more workflow commit notifications failed.", exceptions);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            if (ReferenceEquals(owner._currentScope.Value, this))
                owner._currentScope.Value = parent;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(IWorkflowCommitNotificationScope));
        }
    }

    private record Entry(INotification Notification, IEventPublishingStrategy? Strategy);
}
