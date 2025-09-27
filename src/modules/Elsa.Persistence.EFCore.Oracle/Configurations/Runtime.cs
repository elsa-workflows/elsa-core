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
        builder.Property<string>("SerializedActivityState").HasColumnType("NCLOB");
        builder.Property<string>("SerializedException").HasColumnType("NCLOB");
        builder.Property<string>("SerializedPayload").HasColumnType("NCLOB");
        builder.Property<string>("SerializedOutputs").HasColumnType("NCLOB");
        builder.Property<string>("SerializedProperties").HasColumnType("NCLOB");
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