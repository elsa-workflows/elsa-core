namespace Elsa.Workflows.Api.Endpoints.ActivityDescriptors.Get;

internal class Request
{
    public string TypeName { get; set; } = null!;
    public int? Version { get; set; }
}