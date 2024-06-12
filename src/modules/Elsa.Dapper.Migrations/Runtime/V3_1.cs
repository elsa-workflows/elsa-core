using System.Diagnostics.CodeAnalysis;
using FluentMigrator;
using JetBrains.Annotations;
using static System.Int32;

namespace Elsa.Dapper.Migrations.Runtime;

/// <inheritdoc />
[Migration(20002, "Elsa:Runtime:V3.1")]
[PublicAPI]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public class V3_1 : Migration
{
    /// <inheritdoc />
    public override void Up()
    {
        Create
            .Table("KeyValuePairs")
            .WithColumn("Id").AsString().PrimaryKey()
            .WithColumn("Key").AsString().Indexed()
            .WithColumn("Value").AsString(MaxValue).NotNullable();
    }

    /// <inheritdoc />
    public override void Down()
    {
        Delete.Table("KeyValuePairs");
    }
}