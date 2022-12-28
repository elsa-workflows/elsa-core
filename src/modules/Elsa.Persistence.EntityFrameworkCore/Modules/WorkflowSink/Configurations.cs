using Elsa.Workflows.Sink.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elsa.Persistence.EntityFrameworkCore.Modules.WorkflowSink
{
    public class Configurations :
        IEntityTypeConfiguration<WorkflowSinkEntity>
    {
        public void Configure(EntityTypeBuilder<WorkflowSinkEntity> builder)
        {
            builder.Property<string>("Data");
            
            builder.HasIndex(x => x.Id).HasDatabaseName($"IX_{nameof(WorkflowSinkEntity)}_{nameof(WorkflowSinkEntity.Id)}");
        }
    }
}