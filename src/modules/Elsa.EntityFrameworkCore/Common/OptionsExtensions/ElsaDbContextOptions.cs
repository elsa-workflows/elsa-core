namespace Elsa.EntityFrameworkCore.Common;

public class ElsaDbContextOptions
{
    public string? SchemaName { get; set; }
    public string? MigrationsHistoryTableName { get; set; }
}