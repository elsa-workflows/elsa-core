using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.Management;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.IntegrationTests.Scenarios.ImportAndPublish;

public class ImportAndPublishCronTests : IAsyncDisposable
{
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly TextWriter _testOutput;
    private readonly IServiceProvider _services;

    public ImportAndPublishCronTests()
    {
        _testOutput = TestContext.Current!.Output.StandardOutput;
        _services = new TestApplicationBuilder(TestContext.Current!.Output.StandardOutput)
            .WithCapturingTextWriter(_capturingTextWriter)
            .Build();
    }

    [Test]
    [DisplayName("Cron workflow imported from file should publish successfully.")]
    public async Task ImportAndPublish_ShouldSucceed_WithGoodCron()
    {
        // Populate registries.
        await _services.PopulateRegistriesAsync();

        // Import workflow.
        var workflowDefinition = await _services.ImportWorkflowDefinitionAsync($"Scenarios/ImportAndPublish/Workflows/cron-every-hour.json");

        // Publish.
        IWorkflowDefinitionPublisher workflowDefinitionPublisher = _services.GetRequiredService<IWorkflowDefinitionPublisher>();
        try
        {
            var result = await workflowDefinitionPublisher.PublishAsync(workflowDefinition);

            // Assert.
            await Assert.That(result.Succeeded).IsTrue();
            await Assert.That(result.ValidationErrors).IsEmpty();
        }
        finally
        {
            // Publishing arms a real cron schedule. Retract it while the provider is still alive.
            await workflowDefinitionPublisher.RetractAsync(workflowDefinition.DefinitionId);
        }
    }

    [Test]
    [DisplayName("Cron workflow imported from file should not publish successfully with bad cron expression.")]
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
        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.ValidationErrors).HasSingleItem();
        await Assert.That(result.ValidationErrors.Single().Message).IsEqualTo("Error when parsing cron expression: The given cron expression has an invalid format. Seconds: Value must be a number between 0 and 59 (all inclusive).");
    }

    [Test]
    [DisplayName("Cron workflow with bad cron expression should publish successfully when FailOnValidationErrors is disabled.")]
    public async Task ImportAndPublish_ShouldSucceed_WithBadCronExpression_WhenFailOnValidationErrorsDisabled()
    {
        // Opt out of strict publishing.
        await using var services = (ServiceProvider)new TestApplicationBuilder(_testOutput)
            .WithCapturingTextWriter(_capturingTextWriter)
            .ConfigureElsa(elsa => elsa.UseWorkflowManagement(management => management.UseFailOnValidationErrors(false)))
            .Build();

        // Populate registries.
        await services.PopulateRegistriesAsync();

        // Import workflow.
        var workflowDefinition = await services.ImportWorkflowDefinitionAsync($"Scenarios/ImportAndPublish/Workflows/bad-cron-expression.json");

        // Publish.
        IWorkflowDefinitionPublisher workflowDefinitionPublisher = services.GetRequiredService<IWorkflowDefinitionPublisher>();
        var result = await workflowDefinitionPublisher.PublishAsync(workflowDefinition);

        // Assert: publishing succeeds while the validation error is surfaced as a warning.
        await Assert.That(result.Succeeded).IsTrue();
        await Assert.That(result.ValidationErrors).HasSingleItem();
        await Assert.That(result.ValidationErrors.Single().Message).IsEqualTo("Error when parsing cron expression: The given cron expression has an invalid format. Seconds: Value must be a number between 0 and 59 (all inclusive).");
    }

    public ValueTask DisposeAsync() => TestResourceDisposal.DisposeAsync(_services);
}
