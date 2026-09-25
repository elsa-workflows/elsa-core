using System.Diagnostics.CodeAnalysis;
using FluentMigrator;
using JetBrains.Annotations;
using static System.Int32;

namespace Elsa.Persistence.Dapper.Migrations.Runtime;

/// <inheritdoc />
[Migration(20007, "Elsa:Runtime:V3.7")]
[PublicAPI]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public class V3_7 : Migration
{
    /// <inheritdoc />
    public override void Up()
    {
        Alter.Table("ActivityExecutionRecords").AddColumn("SerializedMetadata").AsString(MaxValue).Nullable();
        Alter.Table("ActivityExecutionRecords").AddColumn("SchedulingActivityExecutionId").AsString().Nullable();
        Alter.Table("ActivityExecutionRecords").AddColumn("SchedulingActivityId").AsString().Nullable();
        Alter.Table("ActivityExecutionRecords").AddColumn("SchedulingWorkflowInstanceId").AsString().Nullable();
        Alter.Table("ActivityExecutionRecords").AddColumn("CallStackDepth").AsInt32().Nullable();
        Alter.Table("ActivityExecutionRecords").AddColumn("AggregateFaultCount").AsInt32().NotNullable().WithDefaultValue(0);
    }

    /// <inheritdoc />
    public override void Down()
    {
        Delete.Column("SerializedMetadata").FromTable("ActivityExecutionRecords");
        Delete.Column("SchedulingActivityExecutionId").FromTable("ActivityExecutionRecords");
        Delete.Column("SchedulingActivityId").FromTable("ActivityExecutionRecords");
        Delete.Column("SchedulingWorkflowInstanceId").FromTable("ActivityExecutionRecords");
        Delete.Column("CallStackDepth").FromTable("ActivityExecutionRecords");
        Delete.Column("AggregateFaultCount").FromTable("ActivityExecutionRecords");
    }
}
