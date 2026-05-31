using Elsa.Secrets.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elsa.Secrets.Persistence.EFCore.Oracle.Configurations;

public class SecretsConfiguration : IEntityTypeConfiguration<Secret>
{
    public void Configure(EntityTypeBuilder<Secret> builder)
    {
        builder.Property<string>("SerializedTags").HasColumnName("Tags").HasColumnType("NCLOB").IsRequired();
        builder.Property<string>("SerializedVersions").HasColumnName("Versions").HasColumnType("NCLOB").IsRequired();
    }
}
