namespace Elsa.ModularPersistence.SqlServer.Options;

/// <summary>
/// Configures the SQL Server modular persistence provider.
/// </summary>
public sealed class SqlServerModularPersistenceOptions
{
    public string ConnectionString { get; set; } = "Server=(localdb)\\MSSQLLocalDB;Database=ElsaModularPersistence;Trusted_Connection=True;TrustServerCertificate=True";

    /// <summary>
    /// Creates additional filtered indexes for declared storage indexes.
    /// </summary>
    public bool UseOptimizedIndexes { get; set; }

    /// <summary>
    /// The maximum time to wait for the transactional schema materialization lock.
    /// </summary>
    public TimeSpan SchemaLockTimeout { get; set; } = TimeSpan.FromSeconds(60);
}
