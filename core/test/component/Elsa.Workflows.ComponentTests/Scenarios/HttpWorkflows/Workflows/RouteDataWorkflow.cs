using Elsa.Http;
using Elsa.Workflows.Activities;
using Microsoft.AspNetCore.Http;

namespace Elsa.Workflows.ComponentTests.Scenarios.HttpWorkflows.Workflows;

public class RouteDataWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        var routeDataVariable = builder.WithVariable<IDictionary<string, object>>();
        
        builder.Root = new Sequence
        {
            Activities =
            [
                new HttpEndpoint
                {
                    Path = new("orders/{id}"),
                    SupportedMethods = new([HttpMethods.Get]),
                    CanStartWorkflow = true,
                    RouteData = new(routeDataVariable)
                },
                new WriteHttpResponse
                {
                    Content = new(context => routeDataVariable.Get(context)!["id"].ToString()),
                    ContentType = new("text/plain")
                }
            ]
        };
    }
}