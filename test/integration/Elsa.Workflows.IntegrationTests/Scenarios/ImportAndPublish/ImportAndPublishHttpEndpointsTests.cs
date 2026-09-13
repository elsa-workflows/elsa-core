using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.Management;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.IntegrationTests.Scenarios.ImportAndPublish;

public class ImportAndPublishHttpEndpointsTests : IAsyncDisposable
{
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly TextWriter _testOutput;
    private readonly IServiceProvider _services;

    public ImportAndPublishHttpEndpointsTests()
    {
        _testOutput = TestContext.Current!.Output.StandardOutput;
        _services = new TestApplicationBuilder(TestContext.Current!.Output.StandardOutput)
            .WithCapturingTextWriter(_capturingTextWriter)
            .ConfigureElsa(configure => configure.UseHttp())
            .Build();
    }

    [Test]
    [DisplayName("Http endpoint workflow imported from file should publish successfully.")]
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
        await Assert.That(result.Succeeded).IsTrue();
        await Assert.That(result.ValidationErrors).IsEmpty();
    }

    [Test]
    [DisplayName("Http endpoint workflow imported from file should not publish successfully with same path and method.")]
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
        await Assert.That(result.Succeeded).IsTrue();
        await Assert.That(result.ValidationErrors).IsEmpty();

        // Import second workflow.
        workflowDefinition = await _services.ImportWorkflowDefinitionAsync($"Scenarios/ImportAndPublish/Workflows/http-workflow.json");

        // Publish second workflow.
        result = await workflowDefinitionPublisher.PublishAsync(workflowDefinition);

        // Assert second workflow.
        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.ValidationErrors).HasSingleItem();
        await Assert.That(result.ValidationErrors.Single().Message).IsEqualTo("The /test path and get method are already in use by another workflow!");
    }

    [Test]
    [DisplayName("Http endpoint workflow with duplicate path and method should publish successfully when FailOnValidationErrors is disabled.")]
    public async Task ImportAndPublish_ShouldSucceed_WithTwoHttpEndpointSamePathMethod_WhenFailOnValidationErrorsDisabled()
    {
        // Opt out of strict publishing.
        await using var services = (ServiceProvider)new TestApplicationBuilder(_testOutput)
            .WithCapturingTextWriter(_capturingTextWriter)
            .ConfigureElsa(configure => configure
                .UseHttp()
                .UseWorkflowManagement(management => management.UseFailOnValidationErrors(false)))
            .Build();

        // Populate registries.
        await services.PopulateRegistriesAsync();

        // Import first workflow.
        var workflowDefinition = await services.ImportWorkflowDefinitionAsync($"Scenarios/ImportAndPublish/Workflows/http-workflow.json");

        // Publish first workflow.
        IWorkflowDefinitionPublisher workflowDefinitionPublisher = services.GetRequiredService<IWorkflowDefinitionPublisher>();
        var result = await workflowDefinitionPublisher.PublishAsync(workflowDefinition);

        // Assert first workflow.
        await Assert.That(result.Succeeded).IsTrue();
        await Assert.That(result.ValidationErrors).IsEmpty();

        // Import second workflow.
        workflowDefinition = await services.ImportWorkflowDefinitionAsync($"Scenarios/ImportAndPublish/Workflows/http-workflow.json");

        // Publish second workflow.
        result = await workflowDefinitionPublisher.PublishAsync(workflowDefinition);

        // Assert: publishing succeeds while the validation error is surfaced as a warning.
        await Assert.That(result.Succeeded).IsTrue();
        await Assert.That(result.ValidationErrors).HasSingleItem();
        await Assert.That(result.ValidationErrors.Single().Message).IsEqualTo("The /test path and get method are already in use by another workflow!");
    }

    public ValueTask DisposeAsync() => TestResourceDisposal.DisposeAsync(_services);
}
