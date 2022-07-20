namespace Elsa.ActivityDefinitions.Endpoints.ActivityDefinitions.List;

public class Request
{
    public string? VersionOptions { get; set; }
    public string? DefinitionIds { get; set; }
    public int? Page { get; set; }
    public int? PageSize { get; set; }
}