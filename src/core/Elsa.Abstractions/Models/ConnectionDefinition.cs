namespace Elsa.Models
{
    public class ConnectionDefinition
    {
        public ConnectionDefinition()
        {
        }

        public ConnectionDefinition(string sourceActivityId, string targetActivityId, string outcome)
        {
            SourceActivityId = sourceActivityId;
            TargetActivityId = targetActivityId;
            Outcome = outcome;
        }
        
        public string? SourceActivityId { get; set; }
        public string? TargetActivityId { get; set; }
        public string? Outcome { get; set; }
    }
}