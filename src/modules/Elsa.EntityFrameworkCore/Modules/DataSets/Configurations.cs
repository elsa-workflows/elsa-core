using Elsa.DataSets.Entities;
using Elsa.EntityFrameworkCore.ValueConverters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elsa.EntityFrameworkCore.Modules.DataSets;

internal class Configurations : IEntityTypeConfiguration<DataSetDefinition>, IEntityTypeConfiguration<LinkedServiceDefinition>
{
    public void Configure(EntityTypeBuilder<DataSetDefinition> builder)
    {
        builder.Property(x => x.DataSet).HasConversion<DataSetDefinitionJsonValueConverter>();
        builder.HasIndex(x => x.Name).HasDatabaseName($"IX_{nameof(DataSetDefinition)}_{nameof(DataSetDefinition.Name)}").IsUnique();
    }

    public void Configure(EntityTypeBuilder<LinkedServiceDefinition> builder)
    {
        builder.Property(x => x.LinkedService).HasConversion<LinkedServiceDefinitionJsonValueConverter>();
        builder.HasIndex(x => x.Name).HasDatabaseName($"IX_{nameof(DataSetDefinition)}_{nameof(DataSetDefinition.Name)}").IsUnique();
    }
}