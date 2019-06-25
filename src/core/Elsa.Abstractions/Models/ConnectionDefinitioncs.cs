namespace Elsa.Models
{
    public class ConnectionDefinition
    {
        public ConnectionDefinition(string sourceActivityId, string targetActivityId, string outcome = null)
            : this(
                new SourceEndpointDefinition(sourceActivityId, outcome),
                new TargetEndpointDefinition(targetActivityId)
            )
        {
        }

        public ConnectionDefinition(SourceEndpointDefinition source, TargetEndpointDefinition target)
        {
            Source = source;
            Target = target;
        }

        public SourceEndpointDefinition Source { get; }
        public TargetEndpointDefinition Target { get; }
    }
}