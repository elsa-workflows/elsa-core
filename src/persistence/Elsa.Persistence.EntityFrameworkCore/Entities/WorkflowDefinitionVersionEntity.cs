using System.Collections.Generic;
using Elsa.Models;
using Elsa.Persistence.EntityFrameworkCore.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elsa.Persistence.EntityFrameworkCore.Entities
{
    public class WorkflowDefinitionVersionEntity
    {
        public int Id { get; set; }
        public string VersionId { get; set; }
        public string DefinitionId { get; set; }
        public int Version { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public Variables Variables { get; set; }
        public bool IsSingleton { get; set; }
        public bool IsDisabled { get; set; }
        public bool IsPublished { get; set; }
        public bool IsLatest { get; set; }
        public ICollection<ActivityDefinitionEntity> Activities { get; set; }
        public ICollection<ConnectionDefinitionEntity> Connections { get; set; }
    }

    public class WorkflowDefinitionVersionEntityConfiguration : IEntityTypeConfiguration<WorkflowDefinitionVersionEntity>
    {
        private readonly IDbContextCustomSchema _dbContextCustomSchema;
        public WorkflowDefinitionVersionEntityConfiguration(IDbContextCustomSchema dbContextCustomSchema)
        {
            _dbContextCustomSchema = dbContextCustomSchema;
        }
        public void Configure(EntityTypeBuilder<WorkflowDefinitionVersionEntity> builder)
        {
            if (_dbContextCustomSchema != null && _dbContextCustomSchema.UseCustomSchema)
            {
                builder.ToTable(nameof(WorkflowDefinitionVersionEntity), _dbContextCustomSchema.CustomDefaultSchema);
            }
        }
    }
}