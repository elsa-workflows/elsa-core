using Elsa.Secrets.Management;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elsa.Secrets.Persistence.EntityFrameworkCore;

/// <summary>
/// EF Core configuration for various entity types. 
/// </summary>
public class Configurations : IEntityTypeConfiguration<Secret>
{
    public void Configure(EntityTypeBuilder<Secret> builder)
    {
        builder.HasIndex(x => x.Name).HasDatabaseName($"IX_{nameof(Secret)}_{nameof(Secret.Name)}");
        builder.HasIndex(x => x.Scope).HasDatabaseName($"IX_{nameof(Secret)}_{nameof(Secret.Scope)}");
        builder.HasIndex(x => x.Version).HasDatabaseName($"IX_{nameof(Secret)}_{nameof(Secret.Version)}");
        builder.HasIndex(x => x.Status).HasDatabaseName($"IX_{nameof(Secret)}_{nameof(Secret.Status)}");
        builder.HasIndex(x => x.ExpiresAt).HasDatabaseName($"IX_{nameof(Secret)}_{nameof(Secret.ExpiresAt)}");
        builder.HasIndex(x => x.LastAccessedAt).HasDatabaseName($"IX_{nameof(Secret)}_{nameof(Secret.LastAccessedAt)}");
        builder.HasIndex(x => x.TenantId).HasDatabaseName($"IX_{nameof(Secret)}_{nameof(Secret.TenantId)}");
    }
}