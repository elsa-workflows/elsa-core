using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.Management;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Scenarios.ImportAndPublish;

public class ImportAndPublishCronTests
{
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly IServiceProvider _services;

    public ImportAndPublishCronTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
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

    [Fact(DisplayName = "Cron workflow with a bad cron expression publishes by default, surfacing the validation error as a warning.")]
    public async Task ImportAndPublish_ShouldSucceedWithWarnings_WithBadCronExpression_WhenLenient()
    {
        // Populate registries.
        await _services.PopulateRegistriesAsync();

        // Import workflow.
        var workflowDefinition = await _services.ImportWorkflowDefinitionAsync($"Scenarios/ImportAndPublish/Workflows/bad-cron-expression.json");

        // Publish.
        IWorkflowDefinitionPublisher workflowDefinitionPublisher = _services.GetRequiredService<IWorkflowDefinitionPublisher>();
        var result = await workflowDefinitionPublisher.PublishAsync(workflowDefinition);

        // Assert: lenient is the default in 3.6.x/3.7.x, so publishing succeeds while still reporting the error.
        Assert.True(result.Succeeded);
        Assert.Single(result.ValidationErrors);
        Assert.Equal("Error when parsing cron expression: The given cron expression has an invalid format. Seconds: Value must be a number between 0 and 59 (all inclusive).", result.ValidationErrors.Single().Message);
    }

    [Fact(DisplayName = "Cron workflow with a bad cron expression fails to publish when FailOnValidationErrors is enabled.")]
    public async Task ImportAndPublish_ShouldFail_WithBadCronExpression_WhenStrict()
    {
        var services = new TestApplicationBuilder(_testOutputHelper)
            .WithCapturingTextWriter(_capturingTextWriter)
            .ConfigureElsa(elsa => elsa.UseWorkflowManagement(management => management.UseFailOnValidationErrors()))
            .Build();

        // Populate registries.
        await services.PopulateRegistriesAsync();

        // Import workflow.
        var workflowDefinition = await services.ImportWorkflowDefinitionAsync($"Scenarios/ImportAndPublish/Workflows/bad-cron-expression.json");

        // Publish.
        IWorkflowDefinitionPublisher workflowDefinitionPublisher = services.GetRequiredService<IWorkflowDefinitionPublisher>();
        var result = await workflowDefinitionPublisher.PublishAsync(workflowDefinition);

        // Assert.
        Assert.False(result.Succeeded);
        Assert.Single(result.ValidationErrors);
        Assert.Equal("Error when parsing cron expression: The given cron expression has an invalid format. Seconds: Value must be a number between 0 and 59 (all inclusive).", result.ValidationErrors.Single().Message);
    }
}