using Elsa.Api.Client.Shared;

namespace Elsa.Api.Client.Extensions;

/// <summary>
/// Contains extension methods for the <see cref="HttpResponseMessage"/> class.
/// </summary>
public static class HttpResponseMessageExtensions
{
    /// <summary>
    /// Gets the workflow instance ID from the response.
    /// </summary>
    public static string? GetWorkflowInstanceId(this HttpResponseMessage response) => response.Headers.TryGetValues(HeaderNames.WorkflowInstanceId, out var values) ? values.FirstOrDefault() : default;
}