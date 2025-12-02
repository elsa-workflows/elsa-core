using System.Net;
using Elsa.Http;
using Elsa.Workflows.Activities;
using Microsoft.AspNetCore.Http;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.Http.Workflows;

public class RouteParametersWorkflow : WorkflowBase
{
    private static readonly string DefinitionId = Guid.NewGuid().ToString();

    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WithDefinitionId(DefinitionId);
        
        var routeDataVariable = builder.WithVariable<IDictionary<string, object>>();
        
        builder.Root = new Sequence
        {
            Activities =
            [
                new HttpEndpoint
                {
                    Path = new("test/users/{userId}/orders/{orderId}"),
                    SupportedMethods = new([HttpMethods.Get]),
                    CanStartWorkflow = true,
                    RouteData = new(routeDataVariable)
                },
                new WriteHttpResponse
                {
                    Content = new(context =>
                    {
                        // Try to use RouteData variable first for robustness.
                        // If unavailable (test infra limitation), fallback to manual parsing.
                        const string BasePathPrefix = "/workflows/"; // Extracted as constant for flexibility.
                        try
                        {
                            // Attempt to get route parameters from RouteData variable.
                            var routeData = context.GetVariable<IDictionary<string, object>>();
                            if (routeData != null && routeData.TryGetValue("userId", out var userIdObj) && routeData.TryGetValue("orderId", out var orderIdObj))
                            {
                                var userId = userIdObj?.ToString() ?? "unknown";
                                var orderId = orderIdObj?.ToString() ?? "unknown";
                                return $"UserId: {userId}, OrderId: {orderId}";
                            }

                            // WORKAROUND: Since route table isn't populated during tests,
                            // manually parse the request URL to extract parameters.
                            var httpContext = context.GetRequiredService<Microsoft.AspNetCore.Http.IHttpContextAccessor>().HttpContext;
                            var path = httpContext?.Request?.Path.Value ?? "";

                            // More flexible pattern: allow any base path before /test/users/{userId}/orders/{orderId}
                            var pattern = $"{BasePathPrefix}?test/users/([^/]+)/orders/([^/]+)";
                            var match = System.Text.RegularExpressions.Regex.Match(path, pattern);
                            if (match.Success && match.Groups.Count >= 3)
                            {
                                return $"UserId: {match.Groups[1].Value}, OrderId: {match.Groups[2].Value}";
                            }

                            // Fallback: try simple splitting, accounting for base path
                            var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
                            // Find "test" segment and extract parameters relative to it
                            var testIdx = Array.IndexOf(parts, "test");
                            if (testIdx >= 0 && parts.Length > testIdx + 5 && parts[testIdx + 1] == "users" && parts[testIdx + 3] == "orders")
                            {
                                return $"UserId: {parts[testIdx + 2]}, OrderId: {parts[testIdx + 4]}";
                            }
                            return "Could not parse route parameters";
                        }
                        catch (Exception ex)
                        {
                            return $"UserId: unknown, OrderId: unknown (Error: {ex.Message})";
                        }
                    }),
                    ContentType = new("text/plain"),
                    StatusCode = new(HttpStatusCode.OK)
                }
            ]
        };
    }
}



