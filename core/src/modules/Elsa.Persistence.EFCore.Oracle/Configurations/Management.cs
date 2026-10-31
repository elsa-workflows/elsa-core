using Elsa.Persistence.EFCore.Oracle;
using Elsa.Workflows.Management.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oracle.EntityFrameworkCore.Metadata;

namespace Elsa.Persistence.EFCore.Oracle.Configurations;

internal class Management :
    IEntityTypeConfiguration<WorkflowDefinition>,
    IEntityTypeConfiguration<WorkflowInstance>,
    IEntityTypeConfiguration<WorkflowDefinitionRegistryGeneration>
{
    public void Configure(EntityTypeBuilder<WorkflowDefinition> builder)
    {
        // In order to use data more than 2000 char we have to use NCLOB.
        // In Oracle, we have to explicitly say the column is NCLOB otherwise it would be considered nvarchar(2000).
        builder.Property<string>("StringData").HasColumnType("JSON");
        builder.Property<string>("Data").HasColumnType("NCLOB");
        builder.Property(x => x.Description).HasColumnType("NCLOB");
        builder.Property(x => x.MaterializerContext).HasColumnType("NCLOB");
        builder.Property(x => x.OriginalSource).HasColumnType("NCLOB");
        builder.Property(x => x.BinaryData).HasColumnType("BLOB");
    }

    public void Configure(EntityTypeBuilder<WorkflowInstance> builder)
    {
        // In order to use data more than 2000 char we have to use NCLOB.
        // In Oracle, we have to explicitly say the column is NCLOB otherwise it would be considered nvarchar(2000).
        builder.Property<string>("Data").HasColumnType("NCLOB");
    }

    public void Configure(EntityTypeBuilder<WorkflowDefinitionRegistryGeneration> builder)
    {
        // TenantId is the PK on every other provider. Oracle forbids NULL in a primary key and stores
        // '' as NULL, so the default tenant cannot use TenantId as the key. A shadow identity PK keeps
        // the row addressable. One row per tenant is the NVL unique index in the migration (EF cannot
        // express NVL, so the snapshot has no unique index on TenantId).
        builder.Property<long>("Id")
            .ValueGeneratedOnAdd()
            .HasColumnType("NUMBER(19)");
        OraclePropertyBuilderExtensions.UseIdentityColumn(builder.Property<long>("Id"));
        builder.HasKey("Id");
        builder.Property(x => x.TenantId)
            .IsRequired(false)
            .HasMaxLength(450)
            .HasColumnType("NVARCHAR2(450)")
            .HasConversion(OracleEmptyStringAsNull.Converter);
    }
}
