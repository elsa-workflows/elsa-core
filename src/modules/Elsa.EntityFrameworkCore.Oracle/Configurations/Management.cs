using Elsa.Workflows.Management.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elsa.EntityFrameworkCore.Oracle.Configurations;

internal class Management : IEntityTypeConfiguration<WorkflowDefinition>, IEntityTypeConfiguration<WorkflowInstance>
{
    public void Configure(EntityTypeBuilder<WorkflowDefinition> builder)
    {
        // In order to use data more than 2000 char we have to use NCLOB.
        // In Oracle, we have to explicitly say the column is NCLOB otherwise it would be considered nvarchar(2000).
        builder.Property<string>("StringData").HasColumnType("NCLOB");
        builder.Property<string>("Data").HasColumnType("NCLOB");
        builder.Property(x => x.Description).HasColumnType("NCLOB");
        builder.Property(x => x.MaterializerContext).HasColumnType("NCLOB");
        builder.Property(x => x.BinaryData).HasColumnType("BLOB");
    }
        
    public void Configure(EntityTypeBuilder<WorkflowInstance> builder)
    {
        // In order to use data more than 2000 char we have to use NCLOB.
        // In Oracle, we have to explicitly say the column is NCLOB otherwise it would be considered nvarchar(2000).
        builder.Property<string>("Data").HasColumnType("NCLOB");
    }
}