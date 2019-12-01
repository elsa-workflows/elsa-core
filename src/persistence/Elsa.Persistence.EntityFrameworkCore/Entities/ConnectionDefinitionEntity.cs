using Elsa.Persistence.EntityFrameworkCore.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elsa.Persistence.EntityFrameworkCore.Entities
{
    public class ConnectionDefinitionEntity
    {
        public int Id { get; set; }
        public WorkflowDefinitionVersionEntity WorkflowDefinitionVersion { get; set; }
        public string SourceActivityId { get; set; }
        public string DestinationActivityId { get; set; }
        public string Outcome { get; set; }
    }

    public class ConnectionDefinitionEntityConfiguration : IEntityTypeConfiguration<ConnectionDefinitionEntity>
    {
        private readonly IDbContextCustomSchema _dbContextCustomSchema;
        public ConnectionDefinitionEntityConfiguration(IDbContextCustomSchema dbContextCustomSchema)
        {
            _dbContextCustomSchema = dbContextCustomSchema;
        }
        public void Configure(EntityTypeBuilder<ConnectionDefinitionEntity> builder)
        {
            if (_dbContextCustomSchema != null && _dbContextCustomSchema.UseCustomSchema)
            {
                builder.ToTable(nameof(ConnectionDefinitionEntity), _dbContextCustomSchema.CustomDefaultSchema);
            }
        }
    }
}