using System.Collections.Generic;
using Elsa.Comparers;
using NodaTime;

namespace Elsa.Models
{
    public class WorkflowInstance
    {
        public WorkflowInstance()
        {
            Variables = new Variables();
            Activities = new List<ActivityInstance>();
            BlockingActivities = new HashSet<BlockingActivity>(new BlockingActivityEqualityComparer());
            ExecutionLog = new List<ExecutionLogEntry>();
            ScheduledActivities = new Stack<ScheduledActivity>();
        }

        public string? Id { get; set; }
        public string? DefinitionId { get; set; }
        public string? CorrelationId { get; set; }
        public int Version { get; set; }
        public Instant CreatedAt { get; set; }

        //This is temporarily commented out because WorkflowInstance.cs of elsa-2.0 doesn't have it originally.
        //It is also commented out in Elsa.Dashboard/Areas/Elsa/Views/WorkflowInstance/Index.html until we figure out if it's needed.
        //public Instant? StartedAt { get; set; }
        //public Instant? FinishedAt { get; set; }
        //public Instant? FaultedAt { get; set; }
        //public Instant? AbortedAt { get; set; }

        public WorkflowStatus Status { get; set; }
        public WorkflowFault? Fault { get; set; }
        public Variables Variables { get; set; }
        public Variable? Output { get; set; }
        // Variables? Input is inserted because of mapping problems and until we figure out what Output is for.
        public Variables? Input { get; set; }
        public ICollection<ExecutionLogEntry> ExecutionLog { get; set; }
        public ICollection<ActivityInstance> Activities { get; set; }
        public HashSet<BlockingActivity> BlockingActivities { get; set; }
        public Stack<ScheduledActivity> ScheduledActivities { get; set; }


    }
}