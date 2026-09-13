using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.IntegrationTests.Scenarios.HttpRequestWithLiquid;

public sealed class Tests : IAsyncDisposable
{
    private readonly IWorkflowRunner _workflowRunner;
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IServiceProvider _services;

    public Tests()
    {
        _services = new TestApplicationBuilder(TestContext.Current!.Output.StandardOutput)
            .WithCapturingTextWriter(_capturingTextWriter)
            .ConfigureElsa(configure => configure.UseHttp())
            .Build();
        _workflowRunner = _services.GetRequiredService<IWorkflowRunner>();
    }

    [Test]
    public async Task GetProducts()
    {
        await _services.PopulateRegistriesAsync();
        RunWorkflowResult result = await _workflowRunner.RunAsync(new JavascriptAndLiquidWorkflow());
        await Assert.That(result.WorkflowState.Status).IsEqualTo(WorkflowStatus.Finished);
        await Assert.That(result.WorkflowState.Incidents).IsEmpty();
        await Assert.That(result.WorkflowState.SubStatus).IsEqualTo(WorkflowSubStatus.Finished);

        var lines = _capturingTextWriter.Lines.ToList();
        await Assert.That(lines).Contains("First product id: 1");
        await Assert.That(lines).Contains("First product price rounded: 13");
        await Assert.That(lines).Contains("First product as json: {\"id\":1,\"price\":12.99}");
        await Assert.That(lines).Contains("Second product id: 2");

        await Assert.That(lines).Contains("Single product id: 2");
        await Assert.That(lines).Contains("Single product as json: {\"id\":2,\"price\":10}");
    }

    public async ValueTask DisposeAsync()
    {
        await TestResourceDisposal.DisposeAsync(_services);
        _capturingTextWriter.Dispose();
        GC.SuppressFinalize(this);
    }
}
