using Elsa.Testing.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.IntegrationTests.Scenarios.SetGetVariables;

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
        await _workflowRunner.RunAsync<SetGetVariableWorkflow>();
        var lines = _capturingTextWriter.Lines.ToList();
        await Assert.That(lines).IsEquivalentTo(new[] { "Line 5" }, TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    [DisplayName("Workflow can reference variables set in previous activities")]
    public async Task Test2()
    {
        await _services.PopulateRegistriesAsync();
        await _workflowRunner.RunAsync<SetGetVariablesWorkflow>();
        var lines = _capturingTextWriter.Lines.ToList();
        await Assert.That(lines).IsEquivalentTo(new[] { "Variable 2: The value of variable 1" }, TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    [DisplayName("Workflow can set and get named variables")]
    public async Task Test3()
    {
        await _services.PopulateRegistriesAsync();
        await _workflowRunner.RunAsync<SetGetNamedVariableWorkflow>();
        var lines = _capturingTextWriter.Lines.ToList();
        await Assert.That(lines).IsEquivalentTo(new[]
        {
            "Foo = Bar",
            "Foo = Baz"
        }, TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    public ValueTask DisposeAsync() => TestResourceDisposal.DisposeAsync(_services);
}
