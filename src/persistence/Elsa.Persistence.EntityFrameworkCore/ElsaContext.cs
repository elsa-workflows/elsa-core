using System;
using System.Collections.Generic;
using Elsa.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
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

        public DbSet<WorkflowDefinition> WorkflowDefinitions { get; set; }
        public DbSet<WorkflowInstance> WorkflowInstances { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            ConfigureWorkflowDefinition(modelBuilder);
            ConfigureWorkflowInstance(modelBuilder);
        }

        private void ConfigureWorkflowDefinition(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<WorkflowDefinition>();

            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.Variables).HasConversion(x => Serialize(x), x => Deserialize<Variables>(x));
            entity.Property(x => x.Activities).HasConversion(x => Serialize(x), x => Deserialize<IReadOnlyCollection<ActivityDefinition>>(x));
            entity.Property(x => x.Connections).HasConversion(x => Serialize(x), x => Deserialize<IReadOnlyCollection<ConnectionDefinition>>(x));
        }

        private void ConfigureWorkflowInstance(ModelBuilder modelBuilder)
        {
            var instantConverter = new ValueConverter<Instant, DateTime>(x => x.ToDateTimeUtc(), x => Instant.FromDateTimeUtc(x));
            var entity = modelBuilder.Entity<WorkflowInstance>();

            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.CreatedAt).HasConversion(instantConverter);
            entity.Property(x => x.FinishedAt).HasConversion(instantConverter);
            entity.Property(x => x.HaltedAt).HasConversion(instantConverter);
            entity.Property(x => x.StartedAt).HasConversion(instantConverter);
            entity.Property(x => x.Activities).HasConversion(x => Serialize(x), x => Deserialize<IReadOnlyDictionary<string, ActivityInstance>>(x));
            entity.Property(x => x.Scopes).HasConversion(x => Serialize(x), x => Deserialize<Stack<WorkflowExecutionScope>>(x));
            entity.Property(x => x.BlockingActivities).HasConversion(x => Serialize(x), x => Deserialize<HashSet<BlockingActivity>>(x));
            entity.Property(x => x.ExecutionLog).HasConversion(x => Serialize(x), x => Deserialize<IReadOnlyCollection<LogEntry>>(x));
            entity.Property(x => x.Fault).HasConversion(x => Serialize(x), x => Deserialize<WorkflowFault>(x));
            entity.Property(x => x.Input).HasConversion(x => Serialize(x), x => Deserialize<Variables>(x));
        }

        private string Serialize(object value) => JsonConvert.SerializeObject(value, serializerSettings);
        private T Deserialize<T>(string json) => JsonConvert.DeserializeObject<T>(json, serializerSettings);
    }
}