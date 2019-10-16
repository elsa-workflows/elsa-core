using Elsa.Models;

namespace Elsa.WorkflowDesigner.Models
{
    public class DesignerConnection
    {
        public DesignerConnection()
        {
        }

        public DesignerConnection(string sourceActivityId, string destinationActivityId, string outcome)
        {
            SourceActivityId = sourceActivityId;
            DestinationActivityId = destinationActivityId;
            Outcome = outcome;
        }

        public DesignerConnection(ConnectionDefinition connectionDefinition) : this(
            connectionDefinition.SourceActivityId,
            connectionDefinition.DestinationActivityId,
            connectionDefinition.Outcome)
        {
        }

        public string SourceActivityId { get; set; }
        public string DestinationActivityId { get; set; }
        public string Outcome { get; set; }
    }
}