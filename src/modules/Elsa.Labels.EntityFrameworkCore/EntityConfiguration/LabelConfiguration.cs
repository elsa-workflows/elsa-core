using Elsa.Labels.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elsa.Labels.EntityFrameworkCore.Configuration;

public class LabelConfiguration : IEntityTypeConfiguration<Label>
{
    public void Configure(EntityTypeBuilder<Label> builder)
    {
    }
}