using Elsa.Http.IntegrationTests.Activities.Workflows;
using Elsa.Http.IntegrationTests.Helpers;
using Elsa.Testing.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Http.IntegrationTests.Activities;

/// <summary>
/// Integration tests for HTTP response activities when HTTP context is lost.
/// </summary>
public class HttpContextLossTests
{
    private readonly WorkflowTestFixture _fixture;

    public HttpContextLossTests(ITestOutputHelper testOutputHelper)
    {
        _fixture = new WorkflowTestFixture(testOutputHelper)
            .ConfigureServices(services =>
            {
                // Register a null HTTP context accessor to simulate context loss
                services.AddSingleton<IHttpContextAccessor>(new NullHttpContextAccessor());
            });
    }

    [Fact(DisplayName = "WriteHttpResponse should record incident when HTTP context is null")]
    public async Task WriteHttpResponse_WithNoHttpContext_ShouldRecordIncident()
    {
        // Act
        var result = await _fixture.RunWorkflowAsync<WriteHttpResponseWithoutHttpContextWorkflow>();

        // Assert
        // Verify an incident was recorded
        Assert.NotEmpty(result.WorkflowState.Incidents);
        
        var incident = result.WorkflowState.Incidents.First();
        Assert.Contains("HTTP context was lost", incident.Message);
        Assert.Contains("background processing, virtual actor, or after a workflow transition", incident.Message);
    }

    [Fact(DisplayName = "WriteFileHttpResponse should record incident when HTTP context is null")]
    public async Task WriteFileHttpResponse_WithNoHttpContext_ShouldRecordIncident()
    {
        // Act
        var result = await _fixture.RunWorkflowAsync<WriteFileHttpResponseWithoutHttpContextWorkflow>();

        // Assert
        // Verify an incident was recorded
        Assert.NotEmpty(result.WorkflowState.Incidents);
        
        var incident = result.WorkflowState.Incidents.First();
        Assert.Contains("HTTP context was lost", incident.Message);
        Assert.Contains("background processing, virtual actor, or after a workflow transition", incident.Message);
    }
}

