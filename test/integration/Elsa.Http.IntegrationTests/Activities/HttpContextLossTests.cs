using Elsa.Http.IntegrationTests.Activities.Workflows;
using Elsa.Http.IntegrationTests.Helpers;
using Elsa.Testing.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Http.IntegrationTests.Activities;

/// <summary>
/// Integration tests for HTTP response activities when HTTP context is lost.
/// </summary>
public class HttpContextLossTests : IAsyncDisposable
{
    private readonly WorkflowTestFixture _fixture;

    public HttpContextLossTests()
    {
        _fixture = new WorkflowTestFixture(TestContext.Current!.Output.StandardOutput)
            .ConfigureServices(services =>
            {
                // Register a null HTTP context accessor to simulate context loss
                services.AddSingleton<IHttpContextAccessor>(new NullHttpContextAccessor());
            });
    }

    public ValueTask DisposeAsync() => _fixture.DisposeAsync();

    [Test]
    [DisplayName("WriteHttpResponse should record incident when HTTP context is null")]
    public async Task WriteHttpResponse_WithNoHttpContext_ShouldRecordIncident()
    {
        // Act
        var result = await _fixture.RunWorkflowAsync<WriteHttpResponseWithoutHttpContextWorkflow>();

        // Assert
        // Verify an incident was recorded
        await Assert.That(result.WorkflowState.Incidents).IsNotEmpty();

        
        var incident = result.WorkflowState.Incidents.First();
        await Assert.That(incident.Message).Contains("HTTP context was lost", StringComparison.CurrentCulture);

        await Assert.That(incident.Message).Contains("background processing, virtual actor, or after a workflow transition", StringComparison.CurrentCulture);

    }

    [Test]
    [DisplayName("WriteFileHttpResponse should record incident when HTTP context is null")]
    public async Task WriteFileHttpResponse_WithNoHttpContext_ShouldRecordIncident()
    {
        // Act
        var result = await _fixture.RunWorkflowAsync<WriteFileHttpResponseWithoutHttpContextWorkflow>();

        // Assert
        // Verify an incident was recorded
        await Assert.That(result.WorkflowState.Incidents).IsNotEmpty();

        
        var incident = result.WorkflowState.Incidents.First();
        await Assert.That(incident.Message).Contains("HTTP context was lost", StringComparison.CurrentCulture);

        await Assert.That(incident.Message).Contains("background processing, virtual actor, or after a workflow transition", StringComparison.CurrentCulture);

    }
}
