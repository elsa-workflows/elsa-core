using FluentMigrator;

namespace Elsa.Dapper.Migrations.Management;

/// <inheritdoc />
[Migration(1, "Initial")]
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
            .WithColumn("IsPublished").AsBoolean().NotNullable();
        
        IfDatabase("Sqlite")
            .Create
            .Table("WorkflowDefinitions")
            .WithColumn("Id").AsString().PrimaryKey()
            .WithColumn("DefinitionId").AsString().NotNullable()
            .WithColumn("Name").AsString().Nullable()
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
            .WithColumn("IsPublished").AsBoolean().NotNullable();
    }

    /// <inheritdoc />
    public override void Down()
    {
        Delete.Table("WorkflowDefinitions");
    }
}