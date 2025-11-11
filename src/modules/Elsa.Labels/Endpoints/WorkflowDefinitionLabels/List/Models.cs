namespace Elsa.Labels.Endpoints.WorkflowDefinitionLabels.List;

public class Request
{
    public string Id { get; set; } = default!;
}

public class Response
{
    public ICollection<string> Items { get; set; } = default!;

}