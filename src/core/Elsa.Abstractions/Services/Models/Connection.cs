namespace Elsa.Services.Models
{
    public class Connection : IConnection
    {
        public Connection(IActivityBlueprint sourceActivity, IActivityBlueprint targetActivity, string sourceOutcome)
            : this(new SourceEndpoint(sourceActivity, sourceOutcome), new TargetEndpoint(targetActivity))
        {
        }

        public Connection(ISourceEndpoint source, ITargetEndpoint target)
        {
            Source = source;
            Target = target;
        }

        public ISourceEndpoint Source { get; set; }
        public ITargetEndpoint Target { get; set; }
    }
}