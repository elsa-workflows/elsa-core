using Elsa.Persistence.EntityFramework.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elsa.Persistence.EntityFramework.Core.Configuration
{
    public class BlockingActivityConfiguration : IEntityTypeConfiguration<BlockingActivityEntity>
    {
        public void Configure(EntityTypeBuilder<BlockingActivityEntity> builder)
        {
        }
    }
}