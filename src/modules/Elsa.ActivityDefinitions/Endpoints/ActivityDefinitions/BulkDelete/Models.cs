namespace Elsa.ActivityDefinitions.Endpoints.ActivityDefinitions.BulkDelete;

public class Request
{
    public ICollection<string> DefinitionIds { get; set; } = default!;
}

public class Response
{
    public int Deleted { get; set; }
}