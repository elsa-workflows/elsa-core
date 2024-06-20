namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.GetManyById;

internal class Request
{
    public ICollection<string> Ids { get; set; } = default!;
}