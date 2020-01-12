namespace Elsa.Services.Models
{
    public class Connection
    {
        public Connection()
        {
        }

        public Connection(IActivity sourceActivity, IActivity targetActivity, string sourceOutcome = OutcomeNames.Done) 
            : this(new SourceEndpoint(sourceActivity, sourceOutcome), new TargetEndpoint(targetActivity))
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