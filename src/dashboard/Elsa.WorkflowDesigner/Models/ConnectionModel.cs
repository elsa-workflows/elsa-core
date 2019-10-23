using Elsa.Models;

namespace Elsa.WorkflowDesigner.Models
{
    public class ConnectionModel
    {
        public ConnectionModel()
        {
        }

        public ConnectionModel(string sourceActivityId, string destinationActivityId, string outcome)
        {
            SourceActivityId = sourceActivityId;
            DestinationActivityId = destinationActivityId;
            Outcome = outcome;
        }

        public ConnectionModel(ConnectionDefinition connectionDefinition) : this(
            connectionDefinition.SourceActivityId,
            connectionDefinition.DestinationActivityId,
            connectionDefinition.Outcome)
        {
        }

        public string? SourceActivityId { get; set; }
        public string? DestinationActivityId { get; set; }
        public string? Outcome { get; set; }
    }
}