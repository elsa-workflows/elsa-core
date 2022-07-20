namespace Elsa.ActivityDefinitions.Endpoints.ActivityDefinitions.Get;

public class Request
{
    public string DefinitionId { get; set; } = default!;
    public string? VersionOptions { get; set; }
}