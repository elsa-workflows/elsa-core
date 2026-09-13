using Elsa.Extensions;
using Elsa.Testing.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.IntegrationTests.Scenarios.JavaScriptNativeVariables;

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
    [DisplayName("The JavaScript activity can access and modify native variables")]
    public async Task Test1()
    {
        await _services.PopulateRegistriesAsync();
        var workflow = await _workflowBuilderFactory.CreateBuilder().BuildWorkflowAsync<JavaScriptWorkflow>();
        await _workflowRunner.RunAsync(workflow);
        var line1 = _capturingTextWriter.Lines.ToList().ElementAt(0);
        var line2 = _capturingTextWriter.Lines.ToList().ElementAt(1);
        var line3 = _capturingTextWriter.Lines.ToList().ElementAt(2);
        await Assert.That(line1).IsEqualTo("Jane Doe");
        await Assert.That(line2).IsEqualTo("Apple, Banana, Orange");
        await Assert.That(line3).IsEqualTo("jane.doe@acme.com");
    }

    public ValueTask DisposeAsync() => TestResourceDisposal.DisposeAsync(_services);
}
