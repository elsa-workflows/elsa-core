using Elsa.Workflows.Runtime.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elsa.Persistence.EFCore.Oracle.Configurations;

public class Runtime :
    IEntityTypeConfiguration<StoredTrigger>,
    IEntityTypeConfiguration<WorkflowExecutionLogRecord>,
    IEntityTypeConfiguration<ActivityExecutionRecord>,
    IEntityTypeConfiguration<StoredBookmark>,
    IEntityTypeConfiguration<WorkflowInboxMessage>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ActivityExecutionRecord> builder)
    {
        // To use data more than 2000 char we have to use NCLOB.
        // In Oracle, we have to explicitly say the column is NCLOB otherwise it would be considered nvarchar(2000).
        builder.Property(x => x.ActivityNodeId).HasColumnType("NCLOB");
        builder.Property<string>("SerializedActivityState").HasColumnType("NCLOB");
        builder.Property<string>("SerializedException").HasColumnType("NCLOB");
        builder.Property<string>("SerializedPayload").HasColumnType("NCLOB");
        builder.Property<string>("SerializedOutputs").HasColumnType("NCLOB");
        builder.Property<string>("SerializedProperties").HasColumnType("NCLOB");
        builder.Property<string>("SerializedMetadata").HasColumnType("NCLOB");

        // V3_7 created these as NVARCHAR2(200). Indexed unbounded strings would otherwise
        // widen to NVARCHAR2(450) and emit a spurious AlterColumn.
        builder.Property(x => x.SchedulingActivityExecutionId).HasColumnType("NVARCHAR2(200)");
        builder.Property(x => x.SchedulingActivityId).HasColumnType("NVARCHAR2(200)");
        builder.Property(x => x.SchedulingWorkflowInstanceId).HasColumnType("NVARCHAR2(200)");
    }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<StoredBookmark> builder)
    {
        // To use data more than 2000 char we have to use NCLOB.
        // In Oracle, we have to explicitly say the column is NCLOB otherwise it would be considered nvarchar(2000).
        // modelBuilder.Entity<StoredBookmark>().Ignore(x => x.Payload);
        // modelBuilder.Entity<StoredBookmark>().Ignore(x => x.Metadata);
        builder.Property<string>("SerializedPayload").HasColumnType("NCLOB");
        builder.Property<string>("SerializedMetadata").HasColumnType("NCLOB");
    }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<StoredTrigger> builder)
    {
        // To use data more than 2000 char we have to use NCLOB.
        // In Oracle, we have to explicitly say the column is NCLOB otherwise it would be considered nvarchar(2000).
        builder.Property<string>("SerializedPayload").HasColumnType("NCLOB");
    }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<WorkflowExecutionLogRecord> builder)
    {
        // To use data more than 2000 char we have to use NCLOB.
        // In Oracle, we have to explicitly say the column is NCLOB otherwise it would be considered nvarchar(2000).
        builder.Property(x => x.ActivityNodeId).HasColumnType("NCLOB");
        builder.Property<string>("SerializedActivityState").HasColumnType("NCLOB");
        builder.Property<string>("SerializedPayload").HasColumnType("NCLOB");
    }

    public void Configure(EntityTypeBuilder<WorkflowInboxMessage> builder)
    {
        // To use data more than 2000 char we have to use NCLOB.
        // In Oracle, we have to explicitly say the column is NCLOB otherwise it would be considered nvarchar(2000).
        builder.Ignore(x => x.Input);
        builder.Ignore(x => x.BookmarkPayload);
        builder.Property<string>("SerializedInput").HasColumnType("NCLOB");
        builder.Property<string>("SerializedBookmarkPayload").HasColumnType("NCLOB");
    }
}