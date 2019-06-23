namespace Elsa.Services.Models
{
    public class ConnectionBlueprint
    {
        public ConnectionBlueprint(string sourceActivityId, string targetActivityId, string outcome = null)
            : this(
                new SourceEndpointBlueprint(sourceActivityId, outcome),
                new TargetEndpointBlueprint(targetActivityId)
            )
        {
        }

        public ConnectionBlueprint(SourceEndpointBlueprint source, TargetEndpointBlueprint target)
        {
            Source = source;
            Target = target;
        }

        public SourceEndpointBlueprint Source { get; }
        public TargetEndpointBlueprint Target { get; }
    }
}