using Elsa.Testing.Shared;
using Elsa.Workflows.Management;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.IntegrationTests.Scenarios.ImportAndPublish;

public class ImportAndPublishTimerTests : IAsyncDisposable
{
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IServiceProvider _services;

    public ImportAndPublishTimerTests()
    {
        _services = new TestApplicationBuilder(TestContext.Current!.Output.StandardOutput)
            .WithCapturingTextWriter(_capturingTextWriter)
            .Build();
    }

    [Test]
    [DisplayName("Timer workflow imported from file should publish successfully.")]
    public async Task ImportAndPublish_ShouldSucceed_WithGoodTimerWithoutValidator()
    {
        // Populate registries.
        await _services.PopulateRegistriesAsync();

        // Import workflow.
        var workflowDefinition = await _services.ImportWorkflowDefinitionAsync($"Scenarios/ImportAndPublish/Workflows/timer-workflow.json");

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
            // Publishing arms a real recurring timer. Retracting exercises the normal unscheduling path before
            // this test disposes the provider that timer would otherwise retain.
            await workflowDefinitionPublisher.RetractAsync(workflowDefinition.DefinitionId);
        }
    }

    public ValueTask DisposeAsync() => TestResourceDisposal.DisposeAsync(_services);
}
