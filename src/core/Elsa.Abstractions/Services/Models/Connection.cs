namespace Elsa.Services.Models
{
    public class Connection : IConnection
    {
        // ReSharper disable once UnusedMember.Global
        // This constructor is necessary for JSOn de-serialization.
        public Connection()
        {
        }

        public Connection(IActivityBlueprint sourceActivity, IActivityBlueprint targetActivity, string sourceOutcome)
            : this(new SourceEndpoint(sourceActivity, sourceOutcome), new TargetEndpoint(targetActivity))
        {
        }

        public Connection(ISourceEndpoint source, ITargetEndpoint target)
        {
            Source = source;
            Target = target;
        }

        public ISourceEndpoint Source { get; init; } = default!;
        public ITargetEndpoint Target { get; init; } = default!;
    }
}