using System.Diagnostics.CodeAnalysis;
using FluentMigrator;
using JetBrains.Annotations;

namespace Elsa.Dapper.Migrations.Management;

/// <inheritdoc />
[Migration(10005, "Elsa:Management:V3.4")]
[PublicAPI]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public class V3_4: Migration
{
    /// <inheritdoc />
    public override void Up()
    {
        Alter.Table("WorkflowInstances").AddColumn("IsExecuting").AsBoolean().NotNullable().WithDefaultValue(false);
    }

    /// <inheritdoc />
    public override void Down()
    {
        Delete.Column("IsExecuting").FromTable("WorkflowInstances");
    }
}