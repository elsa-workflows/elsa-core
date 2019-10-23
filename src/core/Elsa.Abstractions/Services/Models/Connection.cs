namespace Elsa.Services.Models
{
    public class Connection
    {
        public Connection()
        {
        }

        public Connection(IActivity sourceActivity, IActivity targetActivity) 
            : this(new SourceEndpoint(sourceActivity), new TargetEndpoint(targetActivity))
        {
        }
        
        public Connection(IActivity sourceActivity, IActivity targetActivity, string sourceEndpointName) 
            : this(new SourceEndpoint(sourceActivity, sourceEndpointName), new TargetEndpoint(targetActivity))
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