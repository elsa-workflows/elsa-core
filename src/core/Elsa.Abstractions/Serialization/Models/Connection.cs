namespace Elsa.Serialization.Models
{
    public class Connection
    {
        public Connection()
        {
        }

        public Connection(string sourceActivityId, string targetActivityId) 
            : this(new SourceEndpoint(sourceActivityId), new TargetEndpoint(targetActivityId))
        {
        }
        
        public Connection(string sourceActivityId, string targetActivityId, string sourceEndpointName) 
            : this(new SourceEndpoint(sourceActivityId, sourceEndpointName), new TargetEndpoint(targetActivityId))
        {
        }

        public Connection(SourceEndpoint source, TargetEndpoint target)
        {
            Source = source;
            Target = target;
        }
        
        public SourceEndpoint Source { get; set; }
        public TargetEndpoint Target { get; set; }
    }
}