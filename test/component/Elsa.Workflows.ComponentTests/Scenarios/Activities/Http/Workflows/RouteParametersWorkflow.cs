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
                        
                        // Get route parameters from route data
                        var userId = "unknown";
                        var orderId = "unknown";
                        
                        if (routeData is { Count: > 0 })
                        {
                            // Proper route data is available
                            userId = routeData.TryGetValue("userid", out var value) ? value.ToString() ?? "unknown" : "unknown";
                            orderId = routeData.TryGetValue("orderid", out var orderIdValue) ? orderIdValue.ToString() ?? "unknown" : "unknown";
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