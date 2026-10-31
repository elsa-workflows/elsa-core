namespace Shared.Options
{
    public sealed class HealthCheckOptions
    {
        public IEnumerable<HealthCheckEndpointOptions> Endpoints { get; set; } = [];

        public bool Disabled => Endpoints is null || !Endpoints.Any();
    }
}
