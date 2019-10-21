using System.Collections.Generic;
using Elsa.Models;
using Elsa.Persistence.EntityFrameworkCore.Documents;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using NodaTime;
using NodaTime.Serialization.JsonNet;

namespace Elsa.Persistence.EntityFrameworkCore
{
    public class ElsaContext : DbContext
    {
        private readonly JsonSerializerSettings serializerSettings;

        public ElsaContext(DbContextOptions<ElsaContext> options) : base(options)
        {
            serializerSettings = new JsonSerializerSettings().ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
        }

        public DbSet<WorkflowDefinitionVersionDocument> WorkflowDefinitionVersions { get; set; }
        public DbSet<WorkflowInstanceDocument> WorkflowInstances { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            ConfigureWorkflowDefinitionVersion(modelBuilder);
            ConfigureWorkflowInstance(modelBuilder);
        }

        private void ConfigureWorkflowDefinitionVersion(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<WorkflowDefinitionVersionDocument>();

            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.DefinitionId);
            entity.Property(x => x.Variables).HasConversion(x => Serialize(x), x => Deserialize<Variables>(x));
            entity.Property(x => x.Activities)
                .HasConversion(x => Serialize(x), x => Deserialize<IList<ActivityDefinition>>(x));
            entity.Property(x => x.Connections)
                .HasConversion(x => Serialize(x), x => Deserialize<IList<ConnectionDefinition>>(x));
        }

        private void ConfigureWorkflowInstance(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<WorkflowInstanceDocument>();

            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.Status).HasConversion<string>();
            entity.Property(x => x.Activities)
                .HasConversion(
                    x => Serialize(x),
                    x => Deserialize<IDictionary<string, ActivityInstance>>(x)
                );
            entity.Property(x => x.Scopes)
                .HasConversion(
                    x => Serialize(x),
                    x => Deserialize<Stack<WorkflowExecutionScope>>(x)
                );
            entity.Property(x => x.BlockingActivities)
                .HasConversion(
                    x => Serialize(x),
                    x => Deserialize<HashSet<BlockingActivity>>(x)
                );
            entity.Property(x => x.ExecutionLog)
                .HasConversion(
                    x => Serialize(x),
                    x => Deserialize<ICollection<LogEntry>>(x)
                );

            entity.Property(x => x.Fault)
                .HasConversion(
                    x => Serialize(x),
                    x => Deserialize<WorkflowFault>(x)
                );
            entity.Property(x => x.Input)
                .HasConversion(
                    x => Serialize(x),
                    x => Deserialize<Variables>(x)
                );
        }

        private string Serialize(object value)
        {
            return JsonConvert.SerializeObject(value, serializerSettings);
        }

        private T Deserialize<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json, serializerSettings);
        }
    }
}