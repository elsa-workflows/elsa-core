using Elsa.Persistence.EntityFramework.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EntityFramework.Core
{
    public class ElsaContext : DbContext
    {
        public ElsaContext(DbContextOptions<ElsaContext> options) : base(options)
        {
        }
        
        protected ElsaContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<WorkflowDefinitionEntity> WorkflowDefinitions { get; set; } = default!;
        public DbSet<WorkflowInstanceEntity> WorkflowInstances { get; set; } = default!;
        public DbSet<BlockingActivityEntity> BlockingActivities { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ElsaContext).Assembly);
        }
    }
}