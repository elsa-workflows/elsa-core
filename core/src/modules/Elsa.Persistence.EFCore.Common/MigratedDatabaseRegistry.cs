using System.Collections.Concurrent;

// ReSharper disable once CheckNamespace
namespace Elsa.Persistence.EFCore;

/// <summary>
/// Keeps track of the databases that have already been migrated by this process, so that migrations are applied once
/// per database rather than once per tenant.
/// </summary>
public class MigratedDatabaseRegistry
{
    private readonly ConcurrentDictionary<(Type DbContextType, string ConnectionString), byte> _migrated = new();

    /// <summary>
    /// Returns <c>true</c> for the first caller to claim a database, and <c>false</c> for every caller after that.
    /// A database without a connection string cannot be told apart from any other, so it is never claimed.
    /// </summary>
    public bool TryClaim(Type dbContextType, string? connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
            return true;

        return _migrated.TryAdd((dbContextType, connectionString), default);
    }

    /// <summary>
    /// Gives up a claim so that a later attempt can migrate the database after a failed one.
    /// </summary>
    public void Release(Type dbContextType, string? connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
            return;

        _migrated.TryRemove((dbContextType, connectionString), out _);
    }
}
