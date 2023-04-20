using Elsa.Workflows.Core.State;
using Microsoft.AspNetCore.Http;

namespace Elsa.Http.Models;

/// <summary>
/// Provides context about the faulted workflow.
/// </summary>
/// <param name="HttpContext">The HTTP context.</param>
/// <param name="WorkflowState">The faulted workflow state.</param>
/// <param name="CancellationToken">The cancellation token.</param>
public record HttpEndpointFaultedWorkflowContext(HttpContext HttpContext, WorkflowState WorkflowState, CancellationToken CancellationToken);