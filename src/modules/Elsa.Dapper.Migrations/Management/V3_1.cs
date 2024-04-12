using System.Diagnostics.CodeAnalysis;
using FluentMigrator;
using JetBrains.Annotations;

namespace Elsa.Dapper.Migrations.Management;

/// <inheritdoc />
[Migration(10002, "Elsa:Management:V3.1")]
[PublicAPI]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public class V3_1 : Migration
{
    /// <inheritdoc />
    public override void Up()
    {
        Alter
            .Table("WorkflowDefinitions")
            .AddColumn("IsSystem").AsBoolean().Nullable();

        Alter
            .Table("WorkflowInstances")
            .AddColumn("IsSystem").AsBoolean().Nullable();
    }

    /// <inheritdoc />
    public override void Down()
    {
        Delete.Column("IsSystem").FromTable("WorkflowInstances");
        Delete.Column("IsSystem").FromTable("WorkflowDefinitions");
    }
}