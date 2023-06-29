namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.IsNameUnique;

internal class Request
{
    public string Name { get; set; } = default!;
}

internal record Response(bool IsUnique);
