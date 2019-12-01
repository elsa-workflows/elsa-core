using Elsa.Persistence.EntityFrameworkCore.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elsa.Persistence.EntityFrameworkCore.Entities
{
    public class BlockingActivityEntity
    {
        public int Id { get; set; }
        public WorkflowInstanceEntity WorkflowInstance { get; set; }
        public string ActivityId { get; set; }
        public string ActivityType { get; set; }
    }

    public class BlockingActivityEntityConfiguration : IEntityTypeConfiguration<BlockingActivityEntity>
    {
        private readonly IDbContextCustomSchema _dbContextCustomSchema;
        public BlockingActivityEntityConfiguration(IDbContextCustomSchema dbContextCustomSchema)
        {
            _dbContextCustomSchema = dbContextCustomSchema;
        }
        public void Configure(EntityTypeBuilder<BlockingActivityEntity> builder)
        {
            if (_dbContextCustomSchema != null && _dbContextCustomSchema.UseCustomSchema)
            {
                builder.ToTable(nameof(BlockingActivityEntity), _dbContextCustomSchema.CustomDefaultSchema);
            }
        }
    }
}