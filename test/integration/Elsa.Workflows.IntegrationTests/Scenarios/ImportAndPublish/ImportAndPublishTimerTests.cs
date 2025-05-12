using Elsa.Testing.Shared;
using Elsa.Workflows.Management;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Scenarios.ImportAndPublish;

public class ImportAndPublishTimerTests
{
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IServiceProvider _services;

    public ImportAndPublishTimerTests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper)
            .WithCapturingTextWriter(_capturingTextWriter)
            .Build();
    }

    [Fact(DisplayName = "Timer workflow imported from file should publish successfully.")]
    public async Task ImportAndPublish_ShouldSucceed_WithGoodTimerWithoutValidator()
    {
        // Populate registries.
        await _services.PopulateRegistriesAsync();

        // Import workflow.
        var workflowDefinition = await _services.ImportWorkflowDefinitionAsync($"Scenarios/ImportAndPublish/Workflows/timer-workflow.json");

        // Publish.
        IWorkflowDefinitionPublisher workflowDefinitionPublisher = _services.GetRequiredService<IWorkflowDefinitionPublisher>();
        var result = await workflowDefinitionPublisher.PublishAsync(workflowDefinition);

        // Assert.
        Assert.True(result.Succeeded);
        Assert.Empty(result.ValidationErrors);
    }
}