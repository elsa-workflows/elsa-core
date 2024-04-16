using Elsa.Workflows.Activities;
using Microsoft.AspNetCore.Http;

namespace Elsa.Http.Models;

/// <summary>
/// Represents the context for authorizing an HTTP endpoint.
/// </summary>
public record AuthorizeHttpEndpointContext(HttpContext HttpContext, Workflow Workflow, string? Policy = default);