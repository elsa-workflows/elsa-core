using Elsa.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elsa.Persistence.EntityFramework.Core.Configuration
{
    public class WorkflowInstanceConfiguration : IEntityTypeConfiguration<WorkflowInstance>
    {
        public void Configure(EntityTypeBuilder<WorkflowInstance> builder)
        {
            builder.Property(x => x.CreatedAt).HasConversion(ValueConverters.InstantConverter);
            builder.Property(x => x.LastExecutedAt).HasConversion(ValueConverters.InstantConverter);
            builder.Property(x => x.FinishedAt).HasConversion(ValueConverters.InstantConverter);
            builder.Property(x => x.CancelledAt).HasConversion(ValueConverters.InstantConverter);
            builder.Property(x => x.FaultedAt).HasConversion(ValueConverters.InstantConverter);
            builder.Ignore(x => x.Output);
            builder.Ignore(x => x.ActivityData);
            builder.Ignore(x => x.ActivityOutput);
            builder.Ignore(x => x.BlockingActivities);
            builder.Ignore(x => x.Fault);
            builder.Ignore(x => x.ScheduledActivities);
            builder.Ignore(x => x.Scopes);
            builder.Ignore(x => x.Variables);
            builder.Property<string>("Data");
        }
    }
}