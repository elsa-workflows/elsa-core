namespace Shared.Options
{
    public sealed class HealthCheckEndpointOptions
    {
        public string Tag { get; set; } = string.Empty;

        public string Endpoint { get; set; } = string.Empty;
    }
}
