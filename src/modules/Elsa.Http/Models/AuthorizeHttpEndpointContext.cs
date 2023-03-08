using Microsoft.AspNetCore.Http;

namespace Elsa.Http.Models;

public record AuthorizeHttpEndpointContext(HttpContext HttpContext, string WorkflowInstanceId, string? Policy = default);