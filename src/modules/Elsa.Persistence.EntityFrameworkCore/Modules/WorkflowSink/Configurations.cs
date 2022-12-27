using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elsa.Persistence.EntityFrameworkCore.Modules.WorkflowSink
{
    public class Configurations :
        IEntityTypeConfiguration<Workflows.Sink.Models.WorkflowSink>
    {
        public void Configure(EntityTypeBuilder<Workflows.Sink.Models.WorkflowSink> builder)
        {
            builder.Property<string>("Data");
            
            builder.HasIndex(x => x.Id).HasDatabaseName($"IX_{nameof(Workflows.Sink.Models.WorkflowSink)}_{nameof(Workflows.Sink.Models.WorkflowSink.Id)}");
        }
    }
}