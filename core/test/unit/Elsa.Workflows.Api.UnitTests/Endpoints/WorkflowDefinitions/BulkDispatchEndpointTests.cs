using System.Net;
using System.Net.Http.Json;
using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Testing.Shared.Authorization;
using Elsa.UnitTests.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Api.Features;
using Elsa.Workflows.Management;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Requests;
using Elsa.Workflows.Runtime.Responses;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Elsa.Workflows.Api.UnitTests.Endpoints.WorkflowDefinitions;

[Collection(nameof(FastEndpointsCollection))]
public class BulkDispatchEndpointTests : IAsyncLifetime
{
    private readonly IWorkflowDispatcher _dispatcher = Substitute.For<IWorkflowDispatcher>();
    private AuthorizationTestHost _host = null!;

    public async Task InitializeAsync()
    {
        var definitions = Substitute.For<IWorkflowDefinitionService>();
        var workflow = Workflow.FromActivity(new WriteLine("Hello"));
        var graph = new WorkflowGraph(workflow, new ActivityNode(workflow, string.Empty), []);
        definitions.FindWorkflowGraphAsync("definition", VersionOptions.Published, Arg.Any<CancellationToken>())
            .Returns(graph);
        _dispatcher.DispatchAsync(Arg.Any<DispatchWorkflowDefinitionRequest>(), Arg.Any<DispatchWorkflowOptions?>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(DispatchWorkflowResponse.Success());

        _host = await AuthorizationTestHost.StartAsync<WorkflowsApiFeature>(
            endpoint => endpoint.Namespace == "Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.BulkDispatch",
            services =>
            {
                services.AddElsa(elsa => elsa.UseWorkflowsApi());
                services.AddSingleton(definitions);
                services.AddSingleton(_dispatcher);
            });
    }

    public async Task DisposeAsync()
    {
        FastEndpointsResolver.Reset();
        await _host.DisposeAsync();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(101)]
    public async Task InvalidCountDoesNotDispatchAnyInstances(int count)
    {
        using var response = await DispatchAsync(new { count });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await _dispatcher.DidNotReceiveWithAnyArgs().DispatchAsync(default(DispatchWorkflowDefinitionRequest)!, null);
    }

    [Fact]
    public async Task ReturnsAnIdForEveryAcceptedDispatchAtTheLimit()
    {
        using var response = await DispatchAsync(new { count = 100 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<BulkDispatchResult>();
        Assert.Equal(100, result!.WorkflowInstanceIds.Distinct().Count());
        await _dispatcher.ReceivedWithAnyArgs(100).DispatchAsync(default(DispatchWorkflowDefinitionRequest)!, null);
    }

    [Fact]
    public async Task StopsAfterADispatchIsRejected()
    {
        _dispatcher.DispatchAsync(Arg.Any<DispatchWorkflowDefinitionRequest>(), Arg.Any<DispatchWorkflowOptions?>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(DispatchWorkflowResponse.Success(), DispatchWorkflowResponse.UnknownChannel());

        using var response = await DispatchAsync(new { count = 3 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await _dispatcher.ReceivedWithAnyArgs(2).DispatchAsync(default(DispatchWorkflowDefinitionRequest)!, null);
    }

    private Task<HttpResponseMessage> DispatchAsync(object body) =>
        _host.SendAsync(HttpMethod.Post, "/workflow-definitions/definition/bulk-dispatch", "workflows/definitions:execute",
            content: JsonContent.Create(body));

    private sealed record BulkDispatchResult(string[] WorkflowInstanceIds);
}
