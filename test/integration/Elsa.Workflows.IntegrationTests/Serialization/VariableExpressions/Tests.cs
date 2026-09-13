using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management;
using Elsa.Workflows.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.IntegrationTests.Serialization.VariableExpressions;

/// <summary>
/// Contains tests for variable expressions serialization.
/// </summary>
public class Tests : IAsyncDisposable
{
    private readonly IServiceProvider _services;
    private readonly IWorkflowSerializer _workflowSerializer;
    private readonly IWorkflowBuilder _workflowBuilder;
    private readonly IWorkflowRunner _workflowRunner;

    /// <summary>
    /// Initializes a new instance of the <see cref="Tests"/> class.
    /// </summary>
    public Tests()
    {
        _services = new TestApplicationBuilder(TestContext.Current!.Output.StandardOutput).Build();
        _workflowSerializer = _services.GetRequiredService<IWorkflowSerializer>();
        IWorkflowBuilderFactory workflowBuilderFactory = _services.GetRequiredService<IWorkflowBuilderFactory>();
        _workflowBuilder = workflowBuilderFactory.CreateBuilder();
        _workflowRunner = _services.GetRequiredService<IWorkflowRunner>();
    }
    
    /// <summary>
    /// Variable types remain intact after serialization.
    /// </summary>
    [Test]
    [DisplayName("Variable types remain intact after serialization")]
    public async Task Test1()
    {
        var workflow = await _workflowBuilder.BuildWorkflowAsync<SampleWorkflow>();
        var serialized = _workflowSerializer.Serialize(workflow);
        var deserializedWorkflow = _workflowSerializer.Deserialize(serialized);
        var rehydratedWriteLine1 = (WriteLine)((Sequence)deserializedWorkflow.Root).Activities.ElementAt(0);
        var rehydratedNumberActivity1 = (NumberActivity)((Sequence)deserializedWorkflow.Root).Activities.ElementAt(2);

        await Assert.That(rehydratedWriteLine1.Text.Expression!.Value).IsOfType(typeof(Variable<string>));
        await Assert.That(rehydratedNumberActivity1.Number.Expression!.Value).IsOfType(typeof(Variable<int>));

        await _workflowRunner.RunAsync(workflow);
    }

    public ValueTask DisposeAsync() => TestResourceDisposal.DisposeAsync(_services);
}
