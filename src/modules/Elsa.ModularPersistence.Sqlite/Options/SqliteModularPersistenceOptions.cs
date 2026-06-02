namespace Elsa.ModularPersistence.Sqlite.Options;

/// <summary>
/// Configures the SQLite modular persistence provider.
/// </summary>
public sealed class SqliteModularPersistenceOptions
{
    public string ConnectionString { get; set; } = "Data Source=modular-persistence.db";
}
