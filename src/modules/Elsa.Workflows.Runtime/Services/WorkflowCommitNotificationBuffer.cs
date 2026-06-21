using Elsa.Mediator.Contracts;

namespace Elsa.Workflows.Runtime;

/// <inheritdoc />
public class WorkflowCommitNotificationBuffer(IMediator mediator) : IWorkflowCommitNotificationBuffer
{
    private readonly IMediator _mediator = mediator;
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

            foreach (var entry in _entries)
                await owner._mediator.SendAsync(entry.Notification, entry.Strategy, cancellationToken);

            _entries.Clear();
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
