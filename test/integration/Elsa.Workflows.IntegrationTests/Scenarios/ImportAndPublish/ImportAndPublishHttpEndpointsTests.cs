using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.Management;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Scenarios.ImportAndPublish;

public class ImportAndPublishHttpEndpointsTests
{
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IServiceProvider _services;

    public ImportAndPublishHttpEndpointsTests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper)
            .WithCapturingTextWriter(_capturingTextWriter)
            .ConfigureElsa(configure => configure.UseHttp())
            .Build();
    }

    [Fact(DisplayName = "Http endpoint workflow imported from file should publish successfully.")]
    public async Task ImportAndPublish_ShouldSucceed_WithGoodHttpEndpoint()
    {
        // Populate registries.
        await _services.PopulateRegistriesAsync();

        // Import workflow.
        var workflowDefinition = await _services.ImportWorkflowDefinitionAsync($"Scenarios/ImportAndPublish/Workflows/http-workflow.json");

        // Publish.
        IWorkflowDefinitionPublisher workflowDefinitionPublisher = _services.GetRequiredService<IWorkflowDefinitionPublisher>();
        var result = await workflowDefinitionPublisher.PublishAsync(workflowDefinition);

        // Assert.
        Assert.True(result.Succeeded);
        Assert.Empty(result.ValidationErrors);
    }

    [Fact(DisplayName = "Http endpoint workflow imported from file should not publish successfully with same path and method.")]
    public async Task ImportAndPublish_ShouldFailed_WithTwoHttpEndpointSamePathMethod()
    {
        // Populate registries.
        await _services.PopulateRegistriesAsync();

        // Import first workflow.
        var workflowDefinition = await _services.ImportWorkflowDefinitionAsync($"Scenarios/ImportAndPublish/Workflows/http-workflow.json");

        // Publish first workflow.
        IWorkflowDefinitionPublisher workflowDefinitionPublisher = _services.GetRequiredService<IWorkflowDefinitionPublisher>();
        var result = await workflowDefinitionPublisher.PublishAsync(workflowDefinition);

        // Assert first workflow.
        Assert.True(result.Succeeded);
        Assert.Empty(result.ValidationErrors);

        // Import second workflow.
        workflowDefinition = await _services.ImportWorkflowDefinitionAsync($"Scenarios/ImportAndPublish/Workflows/http-workflow.json");

        // Publish second workflow.
        result = await workflowDefinitionPublisher.PublishAsync(workflowDefinition);

        // Assert second workflow.
        Assert.False(result.Succeeded);
        Assert.Single(result.ValidationErrors);
        Assert.Equal("The /test path and get method are already in use by another workflow!", result.ValidationErrors.Single().Message);
    }
}