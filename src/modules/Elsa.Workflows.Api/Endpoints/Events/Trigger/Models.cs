namespace Elsa.Workflows.Api.Endpoints.Events.Trigger;

public class Request
{
    public string EventName { get; set; } = default!;
    public string? CorrelationId { get; set; }
}

public class Response
{
}