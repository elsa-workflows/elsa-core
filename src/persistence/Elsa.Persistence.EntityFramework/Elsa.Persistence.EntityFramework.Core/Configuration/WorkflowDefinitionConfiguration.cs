using Elsa.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elsa.Persistence.EntityFramework.Core.Configuration
{
    public class WorkflowDefinitionConfiguration : IEntityTypeConfiguration<WorkflowDefinition>
    {
        public void Configure(EntityTypeBuilder<WorkflowDefinition> builder)
        {
            builder.Ignore(x => x.Activities);
            builder.Ignore(x => x.Connections);
            builder.Ignore(x => x.Variables);
            builder.Ignore(x => x.CustomAttributes);
            builder.Ignore(x => x.ContextOptions);
            builder.Property<string>("Data");
        }
    }
}