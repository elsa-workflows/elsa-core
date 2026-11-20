using Elsa.Common.Entities;

namespace Elsa.Common.Multitenancy;

/// <summary>
/// Identifies a trusted host operation that provisions or maintains tenant-agnostic records of a specific entity type.
/// </summary>
public static class SharedTenantWriteScope
{
    private static readonly AsyncLocal<Grant?> Current = new();

    /// <summary>
    /// Grants ambient authority for the specified entity type until the returned scope is disposed.
    /// Hosts should enter this scope only at the write boundary of a trusted provisioning or maintenance operation.
    /// </summary>
    public static IDisposable Enter<TEntity>() where TEntity : Entity
    {
        var grant = new Grant(typeof(TEntity), Current.Value);
        Current.Value = grant;
        return grant;
    }

    /// <summary>
    /// Returns whether the current operation explicitly holds authority for the entity type.
    /// </summary>
    public static bool IsGranted(Type entityType)
    {
        for (var grant = Current.Value; grant != null; grant = grant.Parent)
        {
            if (grant.EntityType == entityType && !grant.IsDisposed)
                return true;
        }

        return false;
    }

    private sealed class Grant(Type entityType, Grant? parent) : IDisposable
    {
        public Type EntityType { get; } = entityType;
        public Grant? Parent { get; } = parent;
        private volatile bool _disposed;
        public bool IsDisposed => _disposed;

        public void Dispose()
        {
            _disposed = true;
            if (ReferenceEquals(Current.Value, this))
                Current.Value = Parent;
        }
    }
}
