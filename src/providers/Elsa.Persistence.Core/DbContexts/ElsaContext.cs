using System.Collections.Generic;
using Elsa.Models;
using Elsa.Persistence.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using NodaTime;
using NodaTime.Serialization.JsonNet;

namespace Elsa.Persistence.Core.DbContexts
{
    public class ElsaContext : DbContext
    {
        private readonly JsonSerializerSettings _serializerSettings;

        public ElsaContext(DbContextOptions<ElsaContext> options) : base(options)
        {
            _serializerSettings = new JsonSerializerSettings().ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
        }

        protected ElsaContext(DbContextOptions options) : base(options)
        {
            _serializerSettings = new JsonSerializerSettings().ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
        }

        public DbSet<WorkflowDefinitionVersionEntity> WorkflowDefinitionVersions { get; set; } = default!;
        public DbSet<WorkflowInstanceEntity> WorkflowInstances { get; set; }= default!;
        public DbSet<ActivityDefinitionEntity> ActivityDefinitions { get; set; }= default!;
        public DbSet<ConnectionDefinitionEntity> ConnectionDefinitions { get; set; }= default!;
        public DbSet<ActivityInstanceEntity> ActivityInstances { get; set; }= default!;
        public DbSet<BlockingActivityEntity> BlockingActivities { get; set; }= default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            ConfigureWorkflowDefinitionVersion(modelBuilder);
            ConfigureWorkflowInstance(modelBuilder);
            ConfigureActivityDefinition(modelBuilder);
            ConfigureActivityInstance(modelBuilder);
            ConfigureBlockingActivity(modelBuilder);
            ConfigureScheduledActivity(modelBuilder);
            ConfigureConnectionDefinition(modelBuilder);
        }

        private void ConfigureWorkflowDefinitionVersion(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<WorkflowDefinitionVersionEntity>();

            entity.Property(x => x.Id).UseIdentityColumn();
            entity.Property(x => x.DefinitionId);
            entity.Property(x => x.Variables).HasConversion(x => Serialize(x), x => Deserialize<Variables>(x));
            entity.HasMany(x => x.Activities).WithOne(x => x.WorkflowDefinitionVersion);
            entity.HasMany(x => x.Connections).WithOne(x => x.WorkflowDefinitionVersion);
        }

        private void ConfigureWorkflowInstance(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<WorkflowInstanceEntity>();

            entity.Property(x => x.Id).UseIdentityColumn();
            entity.Property(x => x.Status).HasConversion<string>();
            
            entity
                .Property(x => x.Variables)
                .HasConversion(
                    x => Serialize(x),
                    x => Deserialize<Variables>(x)
                );
            
            entity
                .Property(x => x.ExecutionLog)
                .HasConversion(
                    x => Serialize(x),
                    x => Deserialize<ICollection<ExecutionLogEntry>>(x)
                );

            entity
                .Property(x => x.Fault)
                .HasConversion(
                    x => Serialize(x),
                    x => Deserialize<WorkflowFault>(x)
                );
            
            entity
                .Property(x => x.Input)
                .HasConversion(
                    x => Serialize(x),
                    x => Deserialize<Variables>(x)
                );

            entity
                .HasMany(x => x.Activities)
                .WithOne(x => x.WorkflowInstance);
            
            entity
                .HasMany(x => x.BlockingActivities)
                .WithOne(x => x.WorkflowInstance);
            
            entity
                .HasMany(x => x.ScheduledActivities)
                .WithOne(x => x.WorkflowInstance);
        }
        
        private void ConfigureActivityDefinition(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<ActivityDefinitionEntity>();

            entity.Property(x => x.Id).UseIdentityColumn();

            entity
                .Property(x => x.State)
                .HasConversion(x => Serialize(x), x => Deserialize<Variables>(x));
        }
        
        private void ConfigureConnectionDefinition(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<ConnectionDefinitionEntity>();

            entity.Property(x => x.Id).UseIdentityColumn();
        }
        
        private void ConfigureActivityInstance(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<ActivityInstanceEntity>();

            entity.Property(x => x.Id).UseIdentityColumn();
            
            entity
                .Property(x => x.State)
                .HasConversion(x => Serialize(x), x => Deserialize<Variables>(x));
            
            entity
                .Property(x => x.Output)
                .HasConversion(x => Serialize(x), x => Deserialize<Variables>(x));
        }
        
        private void ConfigureBlockingActivity(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<BlockingActivityEntity>();

            entity.HasKey(x => x.Id);
        }
        
        private void ConfigureScheduledActivity(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<ScheduledActivityEntity>();

            entity.HasKey(x => x.Id);

            entity
                .Property(x => x.Input)
                .HasConversion(x => Serialize(x), x => Deserialize<object>(x));
        }

        private string Serialize(object value)
        {
            return JsonConvert.SerializeObject(value, _serializerSettings);
        }

        private T Deserialize<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json, _serializerSettings);
        }
    }
}