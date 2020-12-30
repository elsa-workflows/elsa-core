using Elsa.Persistence.EntityFrameworkCore.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EntityFrameworkCore.Core
{
    internal class ElsaContext : DbContext
    {
        public ElsaContext(DbContextOptions<ElsaContext> options) : base(options)
        {
        }

        public DbSet<WorkflowDefinitionEntity> WorkflowDefinitions { get; set; } = default!;
        public DbSet<WorkflowInstanceEntity> WorkflowInstances { get; set; } = default!;
        public DbSet<BlockingActivityEntity> BlockingActivities { get; set; } = default!;
    }
}