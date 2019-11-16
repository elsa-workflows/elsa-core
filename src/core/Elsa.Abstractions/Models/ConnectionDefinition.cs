namespace Elsa.Models
{
    public class ConnectionDefinition
    {
        public ConnectionDefinition()
        {
        }

        public ConnectionDefinition(string sourceActivityId, string destinationActivityId, string outcome)
        {
            SourceActivityId = sourceActivityId;
            DestinationActivityId = destinationActivityId;
            Outcome = outcome;
        }
        
        public string SourceActivityId { get; set; }
        public string DestinationActivityId { get; set; }
        public string Outcome { get; set; }
    }
}