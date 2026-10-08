using Elsa.Diagnostics.StructuredLogs.Persistence.Relational.Migrations;
using FluentMigrator;

namespace Elsa.Diagnostics.StructuredLogs.Persistence.Relational.UnitTests;

public class StructuredLogMigrationTests
{
    [Fact]
    public void CreateStructuredLogTablesMigration_HasStableVersion()
    {
        var attribute = typeof(M001CreateStructuredLogTables)
            .GetCustomAttributes(typeof(MigrationAttribute), false)
            .Cast<MigrationAttribute>()
            .Single();

        Assert.Equal(2026051301, attribute.Version);
    }
}
