using Elsa.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elsa.Persistence.EntityFrameworkCore.Configuration
{
    public class WorkflowTriggerConfiguration : IEntityTypeConfiguration<WorkflowTrigger>
    {
        public void Configure(EntityTypeBuilder<WorkflowTrigger> builder)
        {
            builder.HasIndex(x => x.WorkflowDefinitionId).HasDatabaseName($"IX_{nameof(WorkflowTrigger)}_{nameof(WorkflowTrigger.WorkflowDefinitionId)}");
            builder.HasIndex(x => x.Name).HasDatabaseName($"IX_{nameof(WorkflowTrigger)}_{nameof(WorkflowTrigger.Name)}");
            builder.HasIndex(x => x.Hash).HasDatabaseName($"IX_{nameof(WorkflowTrigger)}_{nameof(WorkflowTrigger.Hash)}");
        }
    }
}
