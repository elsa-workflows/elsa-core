using FluentMigrator;
using FluentMigrator.Builders.Create.Table;

namespace Elsa.Persistence.Dapper.Migrations.Runtime;

/// <summary>
/// Persists bookmark delivery attempts and dead letters in the runtime database.
/// </summary>
// Keep the same migration version on 3.10 and 3.11 so upgrades apply the schema once.
[Migration(20009, "Elsa:Runtime:BookmarkQueueRecovery")]
public sealed class V3_10_BookmarkQueueRecovery : Migration
{
    private const string QueueTable = "BookmarkQueueItems";
    private const string DeadLetterTable = "BookmarkQueueDeadLetterItems";

    /// <inheritdoc />
    public override void Up()
    {
        if (!Schema.Table(QueueTable).Column("DeliveryAttempts").Exists())
            Alter.Table(QueueTable).AddColumn("DeliveryAttempts").AsInt32().NotNullable().WithDefaultValue(0);
        if (!Schema.Table(QueueTable).Column("LastErrorType").Exists())
            Alter.Table(QueueTable).AddColumn("LastErrorType").AsString(int.MaxValue).Nullable();
        if (!Schema.Table(QueueTable).Column("LastErrorMessage").Exists())
            Alter.Table(QueueTable).AddColumn("LastErrorMessage").AsString(int.MaxValue).Nullable();
        if (!Schema.Table(QueueTable).Column("LastAttemptedAt").Exists())
        {
            IfDatabase(MigrationDatabases.DateTimeOffsetProviders).Alter.Table(QueueTable).AddColumn("LastAttemptedAt").AsDateTimeOffset().Nullable();
            IfDatabase("Sqlite").Alter.Table(QueueTable).AddColumn("LastAttemptedAt").AsDateTime2().Nullable();
        }

        if (!Schema.Table(DeadLetterTable).Exists())
        {
            CreateDeadLetterTable(IfDatabase(MigrationDatabases.DateTimeOffsetProviders).Create.Table(DeadLetterTable), useDateTimeOffset: true);
            CreateDeadLetterTable(IfDatabase("Sqlite").Create.Table(DeadLetterTable), useDateTimeOffset: false);
        }

        const string originalIdIndex = "IX_BookmarkQueueDeadLetterItems_OriginalQueueItemId";
        const string deadLetteredAtIndex = "IX_BookmarkQueueDeadLetterItems_DeadLetteredAt";
        if (!Schema.Table(DeadLetterTable).Index(originalIdIndex).Exists())
            Create.Index(originalIdIndex).OnTable(DeadLetterTable).OnColumn("OriginalQueueItemId").Unique();
        if (!Schema.Table(DeadLetterTable).Index(deadLetteredAtIndex).Exists())
            Create.Index(deadLetteredAtIndex).OnTable(DeadLetterTable).OnColumn("DeadLetteredAt");
    }

    private static void CreateDeadLetterTable(ICreateTableWithColumnSyntax table, bool useDateTimeOffset)
    {
        var columns = table.WithColumn("Id").AsString().PrimaryKey()
            .WithColumn("TenantId").AsString().Nullable()
            .WithColumn("OriginalQueueItemId").AsString().NotNullable()
            .WithColumn("WorkflowInstanceId").AsString().Nullable()
            .WithColumn("CorrelationId").AsString().Nullable()
            .WithColumn("BookmarkId").AsString().Nullable()
            .WithColumn("StimulusHash").AsString().Nullable()
            .WithColumn("ActivityInstanceId").AsString().Nullable()
            .WithColumn("ActivityTypeName").AsString().Nullable()
            .WithColumn("SerializedOptions").AsString(int.MaxValue).Nullable()
            .WithColumn("Reason").AsString().NotNullable()
            .WithColumn("DeliveryAttempts").AsInt32().NotNullable()
            .WithColumn("LastErrorType").AsString(int.MaxValue).Nullable()
            .WithColumn("LastErrorMessage").AsString(int.MaxValue).Nullable()
            .WithColumn("CanReplay").AsBoolean().NotNullable()
            .WithColumn("ReplayedQueueItemId").AsString().Nullable();

        if (useDateTimeOffset)
        {
            columns.WithColumn("CreatedAt").AsDateTimeOffset().NotNullable()
                .WithColumn("DeadLetteredAt").AsDateTimeOffset().NotNullable()
                .WithColumn("LastAttemptedAt").AsDateTimeOffset().Nullable()
                .WithColumn("ReplayedAt").AsDateTimeOffset().Nullable();
        }
        else
        {
            columns.WithColumn("CreatedAt").AsDateTime2().NotNullable()
                .WithColumn("DeadLetteredAt").AsDateTime2().NotNullable()
                .WithColumn("LastAttemptedAt").AsDateTime2().Nullable()
                .WithColumn("ReplayedAt").AsDateTime2().Nullable();
        }
    }

    /// <inheritdoc />
    public override void Down()
    {
        // Retain queued and dead-lettered data during a rollback.
    }
}
