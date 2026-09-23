using Elsa.Connections.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elsa.Connections.Credentials.Persistence.EFCore;

internal sealed class ConnectionGenerationCleanupConfiguration : IEntityTypeConfiguration<ConnectionGenerationCleanup>
{
    public void Configure(EntityTypeBuilder<ConnectionGenerationCleanup> builder)
    {
        builder.ToTable("ConnectionGenerationCleanups");
        builder.HasKey(x => new { x.ConnectionId, x.GenerationId });
        builder.Property(x => x.ConnectionId).HasMaxLength(200);
        builder.Property(x => x.TenantId).HasMaxLength(200);
        builder.Property(x => x.EnvironmentId).HasMaxLength(200);
        builder.Property(x => x.GenerationId).HasMaxLength(200);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(24).IsRequired();
        builder.Property(x => x.LeaseExpiresAt);
        builder.HasIndex(x => new { x.TenantId, x.EnvironmentId, x.ConnectionId });
    }
}
