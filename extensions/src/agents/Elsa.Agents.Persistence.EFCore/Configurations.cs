using Elsa.Persistence.EFCore.Extensions;
using Elsa.Agents.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elsa.Agents.Persistence.EFCore;

/// <summary>
/// EF Core configuration for various entity types. 
/// </summary>
public class Configurations : IEntityTypeConfiguration<AgentDefinition>
{
    public void Configure(EntityTypeBuilder<AgentDefinition> builder)
    {
        builder.Property(x => x.AgentConfig).HasJsonValueConversion();
        builder.HasIndex(x => x.Name).HasDatabaseName($"IX_{nameof(AgentDefinition)}_{nameof(AgentDefinition.Name)}");
        builder.HasIndex(x => x.TenantId).HasDatabaseName($"IX_{nameof(AgentDefinition)}_{nameof(AgentDefinition.TenantId)}");
    }
}