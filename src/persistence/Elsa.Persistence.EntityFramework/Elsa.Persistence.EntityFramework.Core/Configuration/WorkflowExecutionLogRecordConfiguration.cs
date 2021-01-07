using Elsa.Persistence.EntityFramework.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elsa.Persistence.EntityFramework.Core.Configuration
{
    public class WorkflowExecutionLogRecordConfiguration : IEntityTypeConfiguration<WorkflowExecutionLogRecordEntity>
    {
        public void Configure(EntityTypeBuilder<WorkflowExecutionLogRecordEntity> builder)
        {
            builder.Property(x => x.Timestamp).HasConversion(ValueConverters.InstantConverter);
            builder.Property(x => x.Data).HasConversion(ValueConverters.JObjectConverter);
        }
    }
}