using Elsa.Secrets.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elsa.Secrets.Persistence.EntityFramework.Core.Configuration
{
    public class SecretConfiguration : IEntityTypeConfiguration<Secret>
    {
        public void Configure(EntityTypeBuilder<Secret> builder)
        {
            builder.Ignore(x => x.Properties);
            builder.Property<string>("Data");
            builder.Property(x => x.Id).ValueGeneratedOnAdd();
        }
    }
}
