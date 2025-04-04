using System.Diagnostics.CodeAnalysis;
using FluentMigrator;
using JetBrains.Annotations;
using static System.Int32;

namespace Elsa.Dapper.Migrations.Runtime;

/// <inheritdoc />
[Migration(20006, "Elsa:Runtime:V3.5")]
[PublicAPI]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public class V3_5 : Migration
{
    /// <inheritdoc />
    public override void Up()
    {
        Alter.Table("ActivityExecutionRecords").AddColumn("AggregateFaultCount").AsInt32().NotNullable().WithDefault(0);
    }

    /// <inheritdoc />
    public override void Down()
    {
        Delete.Column("AggregateFaultCount").FromTable("ActivityExecutionRecords");
    }
}