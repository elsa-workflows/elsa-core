namespace Elsa.Models
{
    public class Connection
    {
        public Connection()
        {
        }

        public Connection(IActivity source, IActivity target) 
            : this(new SourceEndpoint(source), new TargetEndpoint(target))
        {
        }
        
        public Connection(IActivity source, string sourceEndpointName, IActivity target) 
            : this(new SourceEndpoint(source, sourceEndpointName), new TargetEndpoint(target))
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