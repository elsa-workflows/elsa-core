namespace Elsa.Http;

/// <summary>
/// Implement this to control what to return to the client in case an unhandled exception occurs while executing the workflow.
/// </summary>
public interface IHttpEndpointFaultHandler
{
    ValueTask HandleAsync(HttpEndpointFaultContext context);
}