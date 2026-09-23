using Elsa.Connections.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elsa.Connections.Credentials.Persistence.EFCore;

internal sealed class IntegrationConnectionConfiguration : IEntityTypeConfiguration<IntegrationConnection>
{
    public void Configure(EntityTypeBuilder<IntegrationConnection> builder)
    {
        builder.ToTable("Connections");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasMaxLength(200);
        builder.Property(x => x.TenantId).HasMaxLength(200);
        builder.Property(x => x.EnvironmentId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ProviderId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ProviderAccountId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.CurrentSecretName).HasMaxLength(200);
        builder.Property(x => x.CurrentGenerationId).HasMaxLength(200);
        builder.Property(x => x.OperationId).HasMaxLength(200);
        builder.Property(x => x.OperationSourceGenerationId).HasMaxLength(200);
        builder.Property(x => x.PlannedSecretName).HasMaxLength(200);
        builder.Property(x => x.PlannedGenerationId).HasMaxLength(200);
        builder.Property(x => x.StagedSecretName).HasMaxLength(200);
        builder.Property(x => x.StagedGenerationId).HasMaxLength(200);
        builder.Property(x => x.LastSafeErrorCode).HasMaxLength(100);
        builder.Property(x => x.OperationStatus).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.HasIndex(x => new { x.TenantId, x.EnvironmentId, x.Id }).IsUnique();
    }
}
