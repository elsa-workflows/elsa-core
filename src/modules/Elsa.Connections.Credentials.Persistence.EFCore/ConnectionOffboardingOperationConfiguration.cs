using Elsa.Connections.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elsa.Connections.Credentials.Persistence.EFCore;

internal sealed class ConnectionOffboardingOperationConfiguration : IEntityTypeConfiguration<ConnectionOffboardingOperation>
{
    public void Configure(EntityTypeBuilder<ConnectionOffboardingOperation> builder)
    {
        builder.ToTable("ConnectionOffboardingOperations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasMaxLength(64);
        builder.Property(x => x.TenantId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.EnvironmentId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ConnectionId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ProviderId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ProviderAccountId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Kind).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.GenerationId).HasMaxLength(200);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.LastSafeErrorCode).HasMaxLength(100);
        builder.HasIndex(x => new { x.TenantId, x.EnvironmentId, x.ConnectionId, x.Status, x.CreatedAt });
        builder.HasIndex(x => new { x.TenantId, x.EnvironmentId, x.ConnectionId, x.GenerationId, x.Status });
        builder.HasIndex(x => new { x.TenantId, x.EnvironmentId, x.Status, x.NextAttemptAt, x.ConnectionId, x.Id }).HasDatabaseName("IX_Offboarding_Retry");
        builder.HasIndex(x => new { x.TenantId, x.EnvironmentId, x.Status, x.LeaseExpiresAt, x.ConnectionId, x.Id }).HasDatabaseName("IX_Offboarding_Lease");
    }
}
