using Elsa.Persistence.VNext.Relational;
using Elsa.Persistence.VNext.SqlServer;
using Elsa.Secrets.Persistence.VNext;

namespace Elsa.Persistence.VNext.UnitTests;

public class SqlServerSchemaPlanningTests
{
    private readonly SecretPersistenceSchemaProvider _schemaProvider = new();
    private readonly RelationalSchemaPlanner _planner = new(new SqlServerTypeMapper());
    private readonly SqlServerSchemaSqlRenderer _renderer = new();

    [Fact]
    public void SqlServerRenderer_ProducesProviderSpecificSchemaFromSecretsIntent()
    {
        var statements = RenderStatements();

        Assert.Contains("IF SCHEMA_ID(N'Elsa') IS NULL EXEC(N'CREATE SCHEMA [Elsa]');", statements);
        Assert.Contains(statements, sql => sql.Contains("CREATE TABLE [Elsa].[Secrets]", StringComparison.Ordinal));
        Assert.Contains(statements, sql => sql.Contains("[Id] nvarchar(450) NOT NULL", StringComparison.Ordinal));
        Assert.Contains(statements, sql => sql.Contains("[Tags] nvarchar(max) NOT NULL", StringComparison.Ordinal));
        Assert.Contains(statements, sql => sql.Contains("[CreatedAt] datetimeoffset NOT NULL", StringComparison.Ordinal));
        Assert.Contains(statements, sql => sql.Contains("CONSTRAINT [PK_Secrets] PRIMARY KEY ([Id])", StringComparison.Ordinal));
        Assert.Contains(statements, sql => sql.Contains("CREATE UNIQUE INDEX [IX_Secret_Name] ON [Elsa].[Secrets] ([Name]);", StringComparison.Ordinal));
    }

    private IReadOnlyList<string> RenderStatements()
    {
        var schema = _schemaProvider.DescribeSchema();
        var plan = _planner.Plan(schema);
        return _renderer.Render(plan);
    }
}
