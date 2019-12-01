using Elsa.Persistence.EntityFrameworkCore.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Newtonsoft.Json.Linq;

namespace Elsa.Persistence.EntityFrameworkCore.Entities
{
    public class ActivityInstanceEntity
    {
        public int Id { get; set; }
        public string ActivityId { get; set; }
        public WorkflowInstanceEntity WorkflowInstance { get; set; }
        public string Type { get; set; }
        public JObject State { get; set; }
        public JObject Output { get; set; }
    }

    public class ActivityInstanceEntityConfiguration : IEntityTypeConfiguration<ActivityInstanceEntity>
    {
        private readonly IDbContextCustomSchema _dbContextCustomSchema;
        public ActivityInstanceEntityConfiguration(IDbContextCustomSchema dbContextCustomSchema)
        {
            _dbContextCustomSchema = dbContextCustomSchema;
        }
        public void Configure(EntityTypeBuilder<ActivityInstanceEntity> builder)
        {
            if (_dbContextCustomSchema != null && _dbContextCustomSchema.UseCustomSchema)
            {
                builder.ToTable(nameof(ActivityInstanceEntity), _dbContextCustomSchema.CustomDefaultSchema);
            }
        }
        //public void Configure(EntityTypeBuilder<ActivityInstanceEntity> builder)
        //{
        //    if (DbContexts.ElsaContext.UseCustomDefaultSchema)
        //    {
        //        builder.ToTable(nameof(ActivityInstanceEntity), DbContexts.ElsaContext.CustomDefaultSchema);
        //    }
        //}
    }
}