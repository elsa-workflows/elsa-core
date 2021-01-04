using Elsa.Persistence.EntityFramework.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elsa.Persistence.EntityFramework.Core.Configuration
{
    public class WorkflowInstanceConfiguration : IEntityTypeConfiguration<WorkflowInstanceEntity>
    {
        public void Configure(EntityTypeBuilder<WorkflowInstanceEntity> builder)
        {
            builder.Property(x => x.CreatedAt).HasConversion(ValueConverters.InstantConverter);
            builder.Property(x => x.LastExecutedAt).HasConversion(ValueConverters.InstantConverter);
            builder.Property(x => x.FinishedAt).HasConversion(ValueConverters.InstantConverter);
            builder.Property(x => x.CancelledAt).HasConversion(ValueConverters.InstantConverter);
            builder.Property(x => x.FaultedAt).HasConversion(ValueConverters.InstantConverter);
        }
    }
}