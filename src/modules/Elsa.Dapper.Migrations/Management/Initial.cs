using FluentMigrator;
using JetBrains.Annotations;

namespace Elsa.Dapper.Migrations.Management;

/// <inheritdoc />
[Migration(10001, "Elsa:Management:Initial")]
[PublicAPI]
public class Initial : Migration
{
    /// <inheritdoc />
    public override void Up()
    {
        IfDatabase("SqlServer", "Oracle", "MySql", "Postgres")
            .Create
            .Table("WorkflowDefinitions")
            .WithColumn("Id").AsString().PrimaryKey()
            .WithColumn("DefinitionId").AsString().NotNullable()
            .WithColumn("Name").AsString().Nullable()
            .WithColumn("ToolVersion").AsString().Nullable()
            .WithColumn("Description").AsString().Nullable()
            .WithColumn("ProviderName").AsString().Nullable()
            .WithColumn("MaterializerName").AsString().NotNullable()
            .WithColumn("MaterializerContext").AsString().Nullable()
            .WithColumn("Props").AsString().NotNullable()
            .WithColumn("UsableAsActivity").AsBoolean().Nullable()
            .WithColumn("StringData").AsString().Nullable()
            .WithColumn("BinaryData").AsBinary().Nullable()
            .WithColumn("CreatedAt").AsDateTimeOffset().NotNullable()
            .WithColumn("Version").AsInt32().NotNullable()
            .WithColumn("IsLatest").AsBoolean().NotNullable()
            .WithColumn("IsReadonly").AsBoolean().NotNullable()
            .WithColumn("IsPublished").AsBoolean().NotNullable();
        

        IfDatabase("Sqlite")
            .Create
            .Table("WorkflowDefinitions")
            .WithColumn("Id").AsString().PrimaryKey()
            .WithColumn("DefinitionId").AsString().NotNullable()
            .WithColumn("Name").AsString().Nullable()
            .WithColumn("ToolVersion").AsString().Nullable()
            .WithColumn("Description").AsString().Nullable()
            .WithColumn("ProviderName").AsString().Nullable()
            .WithColumn("MaterializerName").AsString().NotNullable()
            .WithColumn("MaterializerContext").AsString().Nullable()
            .WithColumn("Props").AsString().NotNullable()
            .WithColumn("UsableAsActivity").AsBoolean().Nullable()
            .WithColumn("StringData").AsString().Nullable()
            .WithColumn("BinaryData").AsBinary().Nullable()
            .WithColumn("CreatedAt").AsDateTime2().NotNullable()
            .WithColumn("Version").AsInt32().NotNullable()
            .WithColumn("IsLatest").AsBoolean().NotNullable()
            .WithColumn("IsReadonly").AsBoolean().NotNullable()
            .WithColumn("IsPublished").AsBoolean().NotNullable();

        IfDatabase("SqlServer", "Oracle", "MySql", "Postgres")
            .Create
            .Table("WorkflowInstances")
            .WithColumn("Id").AsString().PrimaryKey()
            .WithColumn("DefinitionId").AsString().NotNullable()
            .WithColumn("DefinitionVersionId").AsString().NotNullable()
            .WithColumn("Version").AsInt32().NotNullable()
            .WithColumn("WorkflowState").AsString().NotNullable()
            .WithColumn("Status").AsString().NotNullable()
            .WithColumn("SubStatus").AsString().NotNullable()
            .WithColumn("CorrelationId").AsString().Nullable()
            .WithColumn("Name").AsString().Nullable()
            .WithColumn("IncidentCount").AsInt32().NotNullable()
            .WithColumn("CreatedAt").AsDateTimeOffset().NotNullable()
            .WithColumn("UpdatedAt").AsDateTimeOffset().Nullable()
            .WithColumn("FinishedAt").AsDateTimeOffset().Nullable();

        IfDatabase("Sqlite")
            .Create
            .Table("WorkflowInstances")
            .WithColumn("Id").AsString().PrimaryKey()
            .WithColumn("DefinitionId").AsString().NotNullable()
            .WithColumn("DefinitionVersionId").AsString().NotNullable()
            .WithColumn("Version").AsInt32().NotNullable()
            .WithColumn("WorkflowState").AsString().NotNullable()
            .WithColumn("Status").AsString().NotNullable()
            .WithColumn("SubStatus").AsString().NotNullable()
            .WithColumn("CorrelationId").AsString().Nullable()
            .WithColumn("Name").AsString().Nullable()
            .WithColumn("IncidentCount").AsInt32().NotNullable()
            .WithColumn("CreatedAt").AsDateTime2().NotNullable()
            .WithColumn("UpdatedAt").AsDateTime2().Nullable()
            .WithColumn("FinishedAt").AsDateTime2().Nullable();
    }

    /// <inheritdoc />
    public override void Down()
    {
        Delete.Table("WorkflowDefinitions");
        Delete.Table("WorkflowInstances");
    }
}