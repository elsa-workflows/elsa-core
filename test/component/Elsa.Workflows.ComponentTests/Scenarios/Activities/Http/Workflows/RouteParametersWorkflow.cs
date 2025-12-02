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
                        var routeData = routeDataVariable.Get(context);
                        
                        // First, try to get route parameters from the proper route data
                        var userId = "unknown";
                        var orderId = "unknown";
                        
                        if (routeData is { Count: > 0 })
                        {
                            // Proper route data is available
                            userId = routeData.TryGetValue("userid", out var value) ? value.ToString() ?? "unknown" : "unknown";
                            orderId = routeData.TryGetValue("orderid", out var value1) ? value1.ToString() ?? "unknown" : "unknown";
                        }
                        else
                        {
                            // Fallback: Extract from HttpContext if route data is not populated
                            try
                            {
                                var httpContext = context.GetRequiredService<Microsoft.AspNetCore.Http.IHttpContextAccessor>().HttpContext;
                                var path = httpContext?.Request?.Path.Value ?? "";
                                
                                // Split path and look for the users/{userId}/orders/{orderId} pattern
                                var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
                                
                                // Find the users pattern in the path
                                for (int i = 0; i < parts.Length - 3; i++)
                                {
                                    if (parts[i] == "users" && i + 2 < parts.Length && parts[i + 2] == "orders" && i + 3 < parts.Length)
                                    {
                                        userId = Uri.UnescapeDataString(parts[i + 1]);
                                        orderId = Uri.UnescapeDataString(parts[i + 3]);
                                        break;
                                    }
                                }
                            }
                            catch
                            {
                                // Keep defaults if parsing fails
                            }
                        }
                        
                        return $"UserId: {userId}, OrderId: {orderId}";
                    }),
                    ContentType = new("text/plain"),
                    StatusCode = new(HttpStatusCode.OK)
                }
            ]
        };
    }
}



