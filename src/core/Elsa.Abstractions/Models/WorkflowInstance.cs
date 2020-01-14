﻿using System.Collections.Generic;
using Elsa.Comparers;
using NodaTime;

namespace Elsa.Models
{
    public class WorkflowInstance
    {
        public WorkflowInstance()
        {
            Variables = new Variables();
            BlockingActivities = new HashSet<BlockingActivity>(new BlockingActivityEqualityComparer());
            ExecutionLog = new List<ExecutionLogEntry>();
            ScheduledActivities = new Stack<ScheduledActivity>();
        }
        
        public string? Id { get; set; }
        public string? DefinitionId { get; set; }
        public int Version { get; set; }
        public WorkflowStatus Status { get; set; }
        public string? CorrelationId { get; set; }
        public Instant CreatedAt { get; set; }
        public Variables Variables { get; set; }
        public Variable? Output { get; set; }
        public HashSet<BlockingActivity> BlockingActivities { get; set; }
        public ICollection<ExecutionLogEntry> ExecutionLog { get; set; }
        public WorkflowFault? Fault { get; set; }
        public Stack<ScheduledActivity> ScheduledActivities { get; set; }
    }
}