using Elsa.Extensions;
using Elsa.Testing.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.IntegrationTests.Scenarios.CompositesPassingData;

public class Tests : IAsyncDisposable
{
    private readonly IWorkflowRunner _workflowRunner;
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IWorkflowBuilderFactory _workflowBuilderFactory;
    private readonly IServiceProvider _services;

    public Tests()
    {
        _services = new TestApplicationBuilder(TestContext.Current!.Output.StandardOutput)
            .WithCapturingTextWriter(_capturingTextWriter)
            .ConfigureElsa(elsa => elsa.UseJavaScript())
            .Build();
        _workflowBuilderFactory = _services.GetRequiredService<IWorkflowBuilderFactory>();
        _workflowRunner = _services.GetRequiredService<IWorkflowRunner>();
    }

    [Test]
    [DisplayName("The main workflow can capture the result of the composite activity")]
    public async Task Test1()
    {
        await _services.PopulateRegistriesAsync();
        var workflow = await _workflowBuilderFactory.CreateBuilder().BuildWorkflowAsync<AddTextMainWorkflow>();

        // Start workflow.
        await _workflowRunner.RunAsync(workflow);

        // Verify expected output.
        var line = _capturingTextWriter.Lines.ToList().Last();
        await Assert.That(line).IsEqualTo("hi there obi wan");
    }

    public ValueTask DisposeAsync() => TestResourceDisposal.DisposeAsync(_services);
}
