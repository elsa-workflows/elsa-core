namespace Elsa.ModularPersistence.PostgreSql.Options;

/// <summary>
/// Configures the PostgreSQL modular persistence provider.
/// </summary>
public sealed class PostgreSqlModularPersistenceOptions
{
    public string ConnectionString { get; set; } = "Host=localhost;Database=ElsaModularPersistence;Username=postgres;Password=postgres";
}
