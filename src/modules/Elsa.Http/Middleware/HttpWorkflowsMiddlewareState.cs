namespace Elsa.Http.Middleware;

public class HttpWorkflowsMiddlewareState
{
    public string HttpRequestTraceId { get; set; } = Guid.NewGuid().ToString();
}