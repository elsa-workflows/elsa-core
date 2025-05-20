using Elsa.Extensions;
using Elsa.Testing.Shared;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Serialization.ProgrammaticWorkflowsAndDelegates;

/// <summary>
/// Contains tests for variable expressions serialization.
/// </summary>
public class Tests
{
    private readonly IActivitySerializer _activitySerializer;
    private readonly IWorkflowBuilder _workflowBuilder;

    /// <summary>
    /// Initializes a new instance of the <see cref="Tests"/> class.
    /// </summary>
    public Tests(ITestOutputHelper testOutputHelper)
    {
        var serviceProvider = new TestApplicationBuilder(testOutputHelper).Build();
        _activitySerializer = serviceProvider.GetRequiredService<IActivitySerializer>();
        var workflowBuilderFactory = serviceProvider.GetRequiredService<IWorkflowBuilderFactory>();
        _workflowBuilder = workflowBuilderFactory.CreateBuilder();
    }
    
    /// <summary>
    /// Tests that programmatic workflows with inputs using delegates do not throw exceptions when serialized.
    /// </summary>
    [Fact(DisplayName = "Programmatic workflows with inputs using delegates do not throw exceptions when serialized")]
    public async Task Test1()
    {
        var workflow = await _workflowBuilder.BuildWorkflowAsync<GreeterWorkflow>();
        var serializedWorkflow = _activitySerializer.Serialize(workflow);
        
        // If it reached here, the test passed
    }
}