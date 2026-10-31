using Elsa.Common.Multitenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elsa.Persistence.EFCore.Modules.Tenants;

/// <summary>
/// EF Core configuration for <see cref="Tenant"/>. 
/// </summary>
public class Configurations : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.Property(x => x.Name).IsRequired();
        builder.Property(x => x.Configuration).HasConversion<ConfigurationJsonConverter>();
        builder.HasIndex(x => x.Name).HasDatabaseName($"IX_{nameof(Tenant)}_{nameof(Tenant.Name)}");
        builder.HasIndex(x => x.TenantId).HasDatabaseName($"IX_{nameof(Tenant)}_{nameof(Tenant.TenantId)}");
    }
}