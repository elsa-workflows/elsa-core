using Microsoft.AspNetCore.Http;

namespace Elsa.Http.Contracts;

/// <summary>
/// Provides a way to select the workflow instance ID for a request.
/// </summary>
public interface IHttpWorkflowInstanceIdSelector
{
    /// <summary>
    /// The priority of this selector. The selector with the highest priority will be used.
    /// </summary>
    double Priority { get; }
    
    /// <summary>
    /// Returns the workflow instance ID for the specified HTTP context, or <c>null</c> if no workflow instance ID could be found.
    /// </summary>
    ValueTask<string?> GetWorkflowInstanceIdAsync(HttpContext httpContext, CancellationToken cancellationToken = default);
}