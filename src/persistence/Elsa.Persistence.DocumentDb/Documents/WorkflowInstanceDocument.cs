using Elsa.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Elsa.Persistence.DocumentDb.Documents
{
    public class WorkflowInstanceDocument : DocumentBase
    {
        internal const string COLLECTION_NAME = "WorkflowInstance";

        [JsonProperty(PropertyName = "definitionId")]
        public string DefinitionId { get; set; }

        [JsonProperty(PropertyName = "type")] 
        public string Type { get; } = nameof(WorkflowInstanceDocument);

        [JsonProperty(PropertyName = "version")]
        public int Version { get; set; }

        [JsonProperty(PropertyName = "status")]
        public WorkflowStatus Status { get; set; }

        [JsonProperty(PropertyName = "correlationId")]
        public string CorrelationId { get; set; }

        [JsonProperty(PropertyName = "createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty(PropertyName = "startedAt")]
        public DateTime? StartedAt { get; set; }

        [JsonProperty(PropertyName = "finishedAt")]
        public DateTime? FinishedAt { get; set; }

        [JsonProperty(PropertyName = "faultedAt")]
        public DateTime? FaultedAt { get; set; }

        [JsonProperty(PropertyName = "abortedAt")]
        public DateTime? AbortedAt { get; set; }

        [JsonProperty(PropertyName = "activities")]
        public IDictionary<string, ActivityInstance> Activities { get; set; } =
            new Dictionary<string, ActivityInstance>();

        [JsonProperty(PropertyName = "scope")]
        public WorkflowExecutionScope Scope { get; set; }

        [JsonProperty(PropertyName = "input")] 
        public Variables Input { get; set; }

        [JsonProperty(PropertyName = "blockingActivities")]
        public HashSet<BlockingActivity> BlockingActivities { get; set; }

        [JsonProperty(PropertyName = "executionLog")]
        public ICollection<LogEntry> ExecutionLog { get; set; }

        [JsonProperty(PropertyName = "fault")] 
        public WorkflowFault Fault { get; set; }
    }
}
