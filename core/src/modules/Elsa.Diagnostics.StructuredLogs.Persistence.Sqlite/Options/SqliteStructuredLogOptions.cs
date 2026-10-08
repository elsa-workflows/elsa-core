using Elsa.Diagnostics.StructuredLogs.Persistence.Relational.Options;

namespace Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite.Options;

public class SqliteStructuredLogOptions
{
    public string ConnectionString { get; set; } = "Data Source=elsa-structured-logs.db";
    public bool RunMigrationsOnStartup { get; set; } = true;
    public RelationalStructuredLogOptions Relational { get; set; } = new();
}
