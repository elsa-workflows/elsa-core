namespace Elsa.ModularPersistence.SqlServer.Options;

/// <summary>
/// Configures the SQL Server modular persistence provider.
/// </summary>
public sealed class SqlServerModularPersistenceOptions
{
    public string ConnectionString { get; set; } = "Server=(localdb)\\MSSQLLocalDB;Database=ElsaModularPersistence;Trusted_Connection=True;TrustServerCertificate=True";
}
