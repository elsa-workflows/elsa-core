using Elsa.Diagnostics.StructuredLogs.Persistence.Relational.Migrations;
using FluentMigrator;
using System.Threading.Tasks;

namespace Elsa.Diagnostics.StructuredLogs.Persistence.Relational.UnitTests;

public class StructuredLogMigrationTests
{
    [Test]
    public async Task CreateStructuredLogTablesMigration_HasStableVersion()
    {
        var attribute = typeof(M001CreateStructuredLogTables)
            .GetCustomAttributes(typeof(MigrationAttribute), false)
            .Cast<MigrationAttribute>()
            .Single();

        await Assert.That(attribute.Version).IsEqualTo(2026051301);
    }
}
