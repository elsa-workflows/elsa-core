using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Memory;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Serialization.VariableExpressions;

/// <summary>
/// Contains tests for variable expressions serialization.
/// </summary>
public class Tests
{
    private readonly IWorkflowSerializer _workflowSerializer;
    private readonly IWorkflowBuilder _workflowBuilder;
    private readonly IWorkflowRunner _workflowRunner;

    /// <summary>
    /// Initializes a new instance of the <see cref="Tests"/> class.
    /// </summary>
    public Tests(ITestOutputHelper testOutputHelper)
    {
        var serviceProvider = new TestApplicationBuilder(testOutputHelper).Build();
        _workflowSerializer = serviceProvider.GetRequiredService<IWorkflowSerializer>();
        IWorkflowBuilderFactory workflowBuilderFactory = serviceProvider.GetRequiredService<IWorkflowBuilderFactory>();
        _workflowBuilder = workflowBuilderFactory.CreateBuilder();
        _workflowRunner = serviceProvider.GetRequiredService<IWorkflowRunner>();
    }
    
    /// <summary>
    /// Variable types remain intact after serialization.
    /// </summary>
    [Fact(DisplayName = "Variable types remain intact after serialization")]
    public async Task Test1()
    {
        var workflow = await _workflowBuilder.BuildWorkflowAsync<SampleWorkflow>();
        var serialized = _workflowSerializer.Serialize(workflow);
        var deserializedWorkflow = _workflowSerializer.Deserialize(serialized);
        var rehydratedWriteLine1 = (WriteLine)((Sequence)deserializedWorkflow.Root).Activities.ElementAt(0);
        var rehydratedNumberActivity1 = (NumberActivity)((Sequence)deserializedWorkflow.Root).Activities.ElementAt(2);

        Assert.IsType<Variable<string>>(rehydratedWriteLine1.Text.Expression!.Value);
        Assert.IsType<Variable<int>>(rehydratedNumberActivity1.Number.Expression!.Value);

        await _workflowRunner.RunAsync(workflow);
    }
}