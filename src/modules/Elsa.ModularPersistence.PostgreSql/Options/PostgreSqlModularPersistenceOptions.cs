namespace Elsa.ModularPersistence.PostgreSql.Options;

/// <summary>
/// Configures the PostgreSQL modular persistence provider.
/// </summary>
public sealed class PostgreSqlModularPersistenceOptions
{
    public string ConnectionString { get; set; } = "Host=localhost;Database=ElsaModularPersistence;Username=postgres;Password=postgres";

    /// <summary>
    /// Creates additional JSONB expression indexes for declared storage indexes.
    /// </summary>
    public bool UseOptimizedJsonbIndexes { get; set; }

    /// <summary>
    /// The PostgreSQL transaction-scoped advisory lock key used during schema materialization.
    /// </summary>
    public long SchemaLockKey { get; set; } = 76060001;
}
