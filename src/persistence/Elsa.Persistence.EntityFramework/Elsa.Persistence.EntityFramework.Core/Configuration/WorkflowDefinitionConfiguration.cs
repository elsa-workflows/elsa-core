using Elsa.Persistence.EntityFramework.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elsa.Persistence.EntityFramework.Core.Configuration
{
    public class WorkflowDefinitionConfiguration : IEntityTypeConfiguration<WorkflowDefinitionEntity>
    {
        public void Configure(EntityTypeBuilder<WorkflowDefinitionEntity> builder)
        {
        }
    }
}