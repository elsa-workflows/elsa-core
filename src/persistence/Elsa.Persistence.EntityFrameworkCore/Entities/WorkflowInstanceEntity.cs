using System;
using System.Collections.Generic;
using Elsa.Models;
using Elsa.Persistence.EntityFrameworkCore.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elsa.Persistence.EntityFrameworkCore.Entities
{
    public class WorkflowInstanceEntity
    {
        public int Id { get; set; }
        public string InstanceId { get; set; }
        public string DefinitionId { get; set; }
        public int Version { get; set; }
        public WorkflowStatus Status { get; set; }
        public string CorrelationId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
        public DateTime? FaultedAt { get; set; }
        public DateTime? AbortedAt { get; set; }
        public Stack<WorkflowExecutionScope> Scopes { get; set; }
        public Variables Input { get; set; }
        public ICollection<LogEntry> ExecutionLog { get; set; }
        public WorkflowFault Fault { get; set; }
        public ICollection<ActivityInstanceEntity> Activities { get; set; }
        public ICollection<BlockingActivityEntity> BlockingActivities { get; set; }
    }

    public class WorkflowInstanceEntityConfiguration : IEntityTypeConfiguration<WorkflowInstanceEntity>
    {
        private readonly IDbContextCustomSchema _dbContextCustomSchema;
        public WorkflowInstanceEntityConfiguration(IDbContextCustomSchema dbContextCustomSchema)
        {
            _dbContextCustomSchema = dbContextCustomSchema;
        }
        public void Configure(EntityTypeBuilder<WorkflowInstanceEntity> builder)
        {
            if (_dbContextCustomSchema != null && _dbContextCustomSchema.UseCustomSchema)
            {
                builder.ToTable(nameof(WorkflowInstanceEntity), _dbContextCustomSchema.CustomDefaultSchema);
            }
        }
    }
}