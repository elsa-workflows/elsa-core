namespace Elsa.Workflows.Api;

public class ApiEndpointOptions
{
    /// <summary>
    /// The prefix used for API routes.
    /// </summary>
    public string RoutePrefix { get; set; } = "elsa/api";

    /// <summary>
    /// The ASP.NET Core rate limiting policy to apply to Elsa API endpoints. Leave empty to disable Elsa API rate limiting.
    /// </summary>
    public string? RateLimitingPolicyName { get; set; }
}
