namespace Elsa.Serialization.Models
{
    public class Connection
    {
        public Connection()
        {
        }
        
        public Connection(string sourceActivityId, string targetActivityId, string outcome = null)
        {
            Source = new SourceEndpoint(sourceActivityId, outcome);
            Target = new TargetEndpoint(targetActivityId);
        }
        
        public SourceEndpoint Source { get; set; }
        public TargetEndpoint Target { get; set; }
    }
}