using System.Reflection;
using FastEndpoints;
using WorkflowsApiFeature = Elsa.Workflows.Api.Features.WorkflowsApiFeature;

namespace Elsa.Workflows.Api.UnitTests.ActivityExecutions;

public class ActivityExecutionsEndpointTests
{
    [Fact]
    public void GetEndpoint_ExposesPathAndQueryRoutes()
    {
        var endpointType = typeof(WorkflowsApiFeature).Assembly.GetType("Elsa.Workflows.Api.Endpoints.ActivityExecutions.Get.Endpoint", throwOnError: true)!;
        var endpoint = Activator.CreateInstance(endpointType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, [null], null)!;
        var definition = new EndpointDefinition(endpointType, typeof(EmptyRequest), typeof(Runtime.Entities.ActivityExecutionRecord));

        endpointType
            .GetProperty("Definition", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(endpoint, definition);

        endpointType.GetMethod("Configure")!.Invoke(endpoint, null);

        Assert.Contains("/activity-executions/{id}", definition.Routes);
        Assert.Contains("/activity-executions/{*id}", definition.Routes);
        Assert.Contains("/activity-executions/by-id", definition.Routes);
    }
}
