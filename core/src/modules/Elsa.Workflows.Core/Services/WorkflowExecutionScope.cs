namespace Elsa.Workflows;

/// <summary>
/// Owns an execution attempt through its pipeline and all subsequent awaited writes.
/// </summary>
/// <remarks>
/// Nested scopes for the same context share the outer lifetime. Different contexts own separate lifetimes,
/// even when their instance IDs match. All work using a context must be awaited before its owner exits;
/// concurrent execution of the same context and detached work are not supported.
/// </remarks>
public sealed class WorkflowExecutionScope : IDisposable
{
    private static readonly AsyncLocal<WorkflowExecutionScope?> Current = new();
    private static readonly object ContextScopeKey = new();
    private readonly Lifetime _lifetime;
    private readonly WorkflowExecutionScope? _previous;
    private readonly bool _ownsLifetime;
    private bool _disposed;

    private WorkflowExecutionScope(Lifetime lifetime, bool ownsLifetime)
    {
        _lifetime = lifetime;
        _ownsLifetime = ownsLifetime;
        _previous = Current.Value;
        Current.Value = this;
    }

    /// <summary>Begins or joins ownership of this context's complete execution attempt.</summary>
    public static WorkflowExecutionScope Begin(WorkflowExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.TransientProperties.TryGetValue(ContextScopeKey, out var value) && value is Lifetime existing && existing.TryBind(context))
        {
            return new(existing, false);
        }

        for (var current = Current.Value; current != null; current = current._previous)
        {
            if (current._lifetime.TryBind(context))
            {
                return new(current._lifetime, false);
            }
        }

        var lifetime = new Lifetime(context.Id);
        lifetime.TryBind(context);
        return new(lifetime, true);
    }

    /// <summary>
    /// Begins ownership before a runner creates its context. Only the first matching context may bind to
    /// this scope. Use this narrowly around the runner call and its known trailing writes.
    /// </summary>
    public static WorkflowExecutionScope Begin(string workflowInstanceId)
    {
        for (var current = Current.Value; current != null; current = current._previous)
        {
            if (current._lifetime.CanJoinBridge(workflowInstanceId))
            {
                return new(current._lifetime, false);
            }
        }

        return new(new Lifetime(workflowInstanceId), true);
    }

    /// <summary>
    /// Lazily registers a resource with the owning attempt. Repeated registration with the same key reuses
    /// the resource. The resource and its context transient entry are retired when the outer owner exits.
    /// The scope must be bound to a context before registration. Resources must not throw during disposal.
    /// </summary>
    public T GetOrAddResource<T>(object key, Func<T> factory) where T : class, IDisposable
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(factory);
        return _lifetime.GetOrAddResource(key, factory);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Current.Value = _previous;
        if (_ownsLifetime)
        {
            _lifetime.Dispose();
        }
    }

    private sealed class Lifetime(string workflowInstanceId) : IDisposable
    {
        private readonly object _gate = new();
        private readonly Dictionary<object, IDisposable> _resources = new();
        private WorkflowExecutionContext? _context;
        private bool _disposed;

        public bool CanJoinBridge(string instanceId)
        {
            lock (_gate)
            {
                return !_disposed && _context == null && workflowInstanceId == instanceId;
            }
        }

        public bool TryBind(WorkflowExecutionContext context)
        {
            lock (_gate)
            {
                if (_disposed || (_context != null ? !ReferenceEquals(_context, context) : context.Id != workflowInstanceId))
                {
                    return false;
                }

                _context = context;
                context.TransientProperties[ContextScopeKey] = this;
                return true;
            }
        }

        public T GetOrAddResource<T>(object key, Func<T> factory) where T : class, IDisposable
        {
            lock (_gate)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                if (_resources.TryGetValue(key, out var existing))
                {
                    return (T)existing;
                }

                if (_context == null)
                {
                    throw new InvalidOperationException("An execution context must bind before resources can be registered.");
                }

                var resource = factory();
                _resources.Add(key, resource);
                _context.TransientProperties[key] = resource;
                return resource;
            }
        }

        public void Dispose()
        {
            IDisposable[] resources;
            lock (_gate)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                if (_context != null)
                {
                    if (_context.TransientProperties.TryGetValue(ContextScopeKey, out var scope) && ReferenceEquals(scope, this))
                    {
                        _context.TransientProperties.Remove(ContextScopeKey);
                    }
                    foreach (var (key, resource) in _resources)
                    {
                        if (_context.TransientProperties.TryGetValue(key, out var value) && ReferenceEquals(value, resource))
                        {
                            _context.TransientProperties.Remove(key);
                        }
                    }
                }
                _context = null;
                resources = _resources.Values.ToArray();
                _resources.Clear();
            }

            // Disposal can wait for cancellation callbacks; do not hold the lifetime gate while it does.
            foreach (var resource in resources)
            {
                resource.Dispose();
            }
        }
    }
}
