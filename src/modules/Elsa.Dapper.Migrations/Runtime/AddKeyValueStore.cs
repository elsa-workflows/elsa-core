using FluentMigrator;
using JetBrains.Annotations;

namespace Elsa.Dapper.Migrations.Runtime;

/// <inheritdoc />
[Migration(20002, "Elsa:Runtime:AddKeyValueStore")]
[PublicAPI]
public class AddKeyValueStore : Migration
{
    /// <inheritdoc />
    public override void Up()
    {
        Create
            .Table("KeyValuePairs")
            .WithColumn("Key").AsString().PrimaryKey()
            .WithColumn("Value").AsString().NotNullable();
    }

    /// <inheritdoc />
    public override void Down()
    {
        Delete.Table("KeyValuePairs");
    }
}