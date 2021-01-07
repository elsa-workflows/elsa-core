using Elsa.Models;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EntityFramework.Core
{
    public class ElsaContext : DbContext
    {
        public ElsaContext(DbContextOptions<ElsaContext> options) : base(options)
        {
        }

        public DbSet<WorkflowDefinition> WorkflowDefinitions { get; set; } = default!;
        public DbSet<WorkflowInstance> WorkflowInstances { get; set; } = default!;
        public DbSet<WorkflowExecutionLogRecord> WorkflowExecutionLogRecords { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ElsaContext).Assembly);
        }
    }
}