using Elsa.Secrets.Models;
using Elsa.Secrets.Persistence.EFCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elsa.Secrets.Persistence.EFCore.Oracle.Configurations;

public class SecretsConfiguration : IEntityTypeConfiguration<Secret>
{
    public void Configure(EntityTypeBuilder<Secret> builder)
    {
        builder.Property<string>(SecretShadowPropertyNames.SerializedTags).HasColumnName("Tags").HasColumnType("NCLOB").IsRequired();
        builder.Property<string>(SecretShadowPropertyNames.SerializedVersions).HasColumnName("Versions").HasColumnType("NCLOB").IsRequired();
    }
}
