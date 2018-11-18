namespace Flowsharp.Models
{
    public class Connection
    {
        public Connection()
        {
        }

        public Connection(Flowsharp.IActivity source, Flowsharp.IActivity target) : this(new SourceEndpoint(source), new TargetEndpoint(target))
        {
        }
        
        public Connection(Flowsharp.IActivity source, string sourceEndpointName, Flowsharp.IActivity target) : this(new SourceEndpoint(source, sourceEndpointName), new TargetEndpoint(target))
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