using Elsa.Agents.Persistence.Entities;
using Elsa.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elsa.Agents.Persistence.EntityFrameworkCore;

/// <summary>
/// EF Core configuration for various entity types. 
/// </summary>
public class Configurations : IEntityTypeConfiguration<ApiKeyDefinition>, IEntityTypeConfiguration<ServiceDefinition>, IEntityTypeConfiguration<AgentDefinition>
{
    public void Configure(EntityTypeBuilder<ApiKeyDefinition> builder)
    {
        builder.HasIndex(x => x.Name).HasDatabaseName($"IX_{nameof(ApiKeyDefinition)}_{nameof(ApiKeyDefinition.Name)}");
        builder.HasIndex(x => x.TenantId).HasDatabaseName($"IX_{nameof(ApiKeyDefinition)}_{nameof(ApiKeyDefinition.TenantId)}");
    }

    public void Configure(EntityTypeBuilder<ServiceDefinition> builder)
    {
        builder.Property(x => x.Settings).HasJsonValueConversion();
        builder.HasIndex(x => x.Name).HasDatabaseName($"IX_{nameof(ServiceDefinition)}_{nameof(ServiceDefinition.Name)}");
        builder.HasIndex(x => x.TenantId).HasDatabaseName($"IX_{nameof(ServiceDefinition)}_{nameof(ServiceDefinition.TenantId)}");
    }

    public void Configure(EntityTypeBuilder<AgentDefinition> builder)
    {
        builder.Property(x => x.AgentConfig).HasJsonValueConversion();
        builder.HasIndex(x => x.Name).HasDatabaseName($"IX_{nameof(AgentDefinition)}_{nameof(AgentDefinition.Name)}");
        builder.HasIndex(x => x.TenantId).HasDatabaseName($"IX_{nameof(AgentDefinition)}_{nameof(AgentDefinition.TenantId)}");
    }
}