using Elsa.Testing.Shared;
using Elsa.Workflows.Management;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Scenarios.ImportAndPublish;

public class ImportAndPublishCronTests
{
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IServiceProvider _services;

    public ImportAndPublishCronTests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper)
            .WithCapturingTextWriter(_capturingTextWriter)
            .Build();
    }

    [Fact(DisplayName = "Cron workflow imported from file should publish successfully.")]
    public async Task ImportAndPublish_ShouldSucceed_WithGoodCron()
    {
        // Populate registries.
        await _services.PopulateRegistriesAsync();

        // Import workflow.
        var workflowDefinition = await _services.ImportWorkflowDefinitionAsync($"Scenarios/ImportAndPublish/Workflows/cron-every-hour.json");

        // Publish.
        IWorkflowDefinitionPublisher workflowDefinitionPublisher = _services.GetRequiredService<IWorkflowDefinitionPublisher>();
        var result = await workflowDefinitionPublisher.PublishAsync(workflowDefinition);

        // Assert.
        Assert.True(result.Succeeded);
        Assert.Empty(result.ValidationErrors);
    }

    [Fact(DisplayName = "Cron workflow imported from file should not publish successfully with bad cron expression.")]
    public async Task ImportAndPublish_ShouldFailed_WithBadCronExpression()
    {
        // Populate registries.
        await _services.PopulateRegistriesAsync();

        // Import workflow.
        var workflowDefinition = await _services.ImportWorkflowDefinitionAsync($"Scenarios/ImportAndPublish/Workflows/bad-cron-expression.json");

        // Publish.
        IWorkflowDefinitionPublisher workflowDefinitionPublisher = _services.GetRequiredService<IWorkflowDefinitionPublisher>();
        var result = await workflowDefinitionPublisher.PublishAsync(workflowDefinition);

        // Assert.
        Assert.False(result.Succeeded);
        Assert.Single(result.ValidationErrors);
        Assert.Equal("Error when parsing cron expression: The given cron expression has an invalid format. Seconds: Value must be a number between 0 and 59 (all inclusive).", result.ValidationErrors.Single().Message);
    }
}