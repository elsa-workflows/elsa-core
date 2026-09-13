using Elsa.Testing.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.IntegrationTests.Scenarios.SetGetVariablesArray;

public class Tests : IAsyncDisposable
{
    private readonly IWorkflowRunner _workflowRunner;
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IServiceProvider _services;

    public Tests()
    {
        _services = new TestApplicationBuilder(TestContext.Current!.Output.StandardOutput).WithCapturingTextWriter(_capturingTextWriter).Build();
        _services.GetRequiredService<IWorkflowBuilderFactory>();
        _workflowRunner = _services.GetRequiredService<IWorkflowRunner>();
    }

    [Test]
    [DisplayName("Workflow can set variable")]
    public async Task Test1()
    {
        await _services.PopulateRegistriesAsync();
        await _workflowRunner.RunAsync<SetGetVariableArrayWorkflow>();
        var lines = _capturingTextWriter.Lines.ToList();
        await Assert.That(lines).IsEquivalentTo(new[] { "Line 1", "Line 2" }, TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    public ValueTask DisposeAsync() => TestResourceDisposal.DisposeAsync(_services);
}
