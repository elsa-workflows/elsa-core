using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Activities.Flowchart.Activities;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Memory;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.IntegrationTests.Serialization.VariableExpressions;

/// <summary>
/// Contains tests for variable expressions serialization.
/// </summary>
public class Tests
{
    private readonly IWorkflowSerializer _workflowSerializer;
    private readonly IWorkflowBuilder _workflowBuilder;

    public Tests(ITestOutputHelper testOutputHelper)
    {
        var serviceProvider = new TestApplicationBuilder(testOutputHelper).Build();
        _workflowSerializer = serviceProvider.GetRequiredService<IWorkflowSerializer>();
        IWorkflowBuilderFactory workflowBuilderFactory = serviceProvider.GetRequiredService<IWorkflowBuilderFactory>();
        _workflowBuilder = workflowBuilderFactory.CreateBuilder();
    }
    
    [Fact(DisplayName = "Variable types remain intact after serialization")]
    public async Task Test1()
    {
        var workflow = await _workflowBuilder.BuildWorkflowAsync<SampleWorkflow>();
        var serialized = _workflowSerializer.Serialize(workflow);
        var deserializedWorkflow = _workflowSerializer.Deserialize(serialized);
        var rehydratedWriteLine = (WriteLine)((Flowchart)deserializedWorkflow.Root).Activities.ElementAt(0);

        Assert.IsType<Variable<string>>(rehydratedWriteLine.Text.Expression!.Value);
    }
}