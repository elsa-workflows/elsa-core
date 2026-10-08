namespace Elsa.Workflows.Api.Endpoints.Tasks.Complete;

public class Request
{
    public string TaskId { get; set; } = default!;
    public object? Result { get; set; }
}

public class Response
{
}