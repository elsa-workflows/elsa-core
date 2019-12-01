using Elsa.Persistence.EntityFrameworkCore.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Newtonsoft.Json.Linq;

namespace Elsa.Persistence.EntityFrameworkCore.Entities
{
    public class ActivityDefinitionEntity
    {
        public int Id { get; set; }
        public string ActivityId { get; set; }
        public WorkflowDefinitionVersionEntity WorkflowDefinitionVersion { get; set; }
        public string Type { get; set; }
        public int Left { get; set; }
        public int Top { get; set; }
        public JObject State { get; set; }
    }

    public class ActivityDefinitionEntityConfiguration : IEntityTypeConfiguration<ActivityDefinitionEntity>
    {
        private readonly IDbContextCustomSchema _dbContextCustomSchema;
        public ActivityDefinitionEntityConfiguration(IDbContextCustomSchema dbContextCustomSchema)
        {
            _dbContextCustomSchema = dbContextCustomSchema;
        }
        public void Configure(EntityTypeBuilder<ActivityDefinitionEntity> builder)
        {
            if(_dbContextCustomSchema != null && _dbContextCustomSchema.UseCustomSchema)
            {
                builder.ToTable(nameof(ActivityDefinitionEntity), _dbContextCustomSchema.CustomDefaultSchema);
            }
        }
    }
}