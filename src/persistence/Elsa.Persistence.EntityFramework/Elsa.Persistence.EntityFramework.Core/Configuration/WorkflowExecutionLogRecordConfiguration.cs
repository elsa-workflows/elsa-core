using Elsa.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elsa.Persistence.EntityFramework.Core.Configuration
{
    public class WorkflowExecutionLogRecordConfiguration : IEntityTypeConfiguration<WorkflowExecutionLogRecord>
    {
        public void Configure(EntityTypeBuilder<WorkflowExecutionLogRecord> builder)
        {
            builder.Property(x => x.Timestamp).HasConversion(ValueConverters.InstantConverter);
            builder.Property(x => x.Data).HasConversion(ValueConverters.JObjectConverter);
        }
    }
}