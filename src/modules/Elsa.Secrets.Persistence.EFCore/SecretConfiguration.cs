using Elsa.Secrets.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elsa.Secrets.Persistence.EFCore;

internal class SecretConfiguration : IEntityTypeConfiguration<Secret>
{
    public void Configure(EntityTypeBuilder<Secret> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Ignore(x => x.Tags);
        builder.Ignore(x => x.Versions);
        builder.Ignore(x => x.LatestActiveVersion);
        builder.Property<string>(SecretShadowPropertyNames.SerializedTags).HasColumnName("Tags").IsRequired();
        builder.Property<string>(SecretShadowPropertyNames.SerializedVersions).HasColumnName("Versions").IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property<string>(SecretShadowPropertyNames.NormalizedName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.TypeName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.StoreName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Scope).HasMaxLength(200);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.HasIndex(SecretShadowPropertyNames.NormalizedName).HasDatabaseName($"IX_{nameof(Secret)}_{SecretShadowPropertyNames.NormalizedName}").IsUnique();
        builder.HasIndex(x => x.TypeName).HasDatabaseName($"IX_{nameof(Secret)}_{nameof(Secret.TypeName)}");
        builder.HasIndex(x => x.StoreName).HasDatabaseName($"IX_{nameof(Secret)}_{nameof(Secret.StoreName)}");
        builder.HasIndex(x => x.Scope).HasDatabaseName($"IX_{nameof(Secret)}_{nameof(Secret.Scope)}");
        builder.HasIndex(x => x.Status).HasDatabaseName($"IX_{nameof(Secret)}_{nameof(Secret.Status)}");
    }
}
