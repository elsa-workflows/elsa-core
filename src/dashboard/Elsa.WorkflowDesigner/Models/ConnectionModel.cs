using Elsa.Models;

namespace Elsa.WorkflowDesigner.Models
{
    public class ConnectionModel
    {
        public ConnectionModel()
        {
        }

        public ConnectionModel(string sourceActivityId, string targetActivityId, string outcome)
        {
            SourceActivityId = sourceActivityId;
            TargetActivityId = targetActivityId;
            Outcome = outcome;
        }

        public ConnectionModel(ConnectionDefinition connectionDefinition) : this(
            connectionDefinition.SourceActivityId,
            connectionDefinition.TargetActivityId,
            connectionDefinition.Outcome)
        {
        }

        public string? SourceActivityId { get; set; }
        public string? TargetActivityId { get; set; }
        public string? Outcome { get; set; }
    }
}