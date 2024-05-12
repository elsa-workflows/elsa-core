using Elsa.Testing.Shared;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.State;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Scenarios.JsonObjectSerialization;

/// <summary>
/// Tests for serializing and deserializing JSON objects containing reserved keywords such as $id.
/// </summary>
public class Tests
{
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IServiceProvider _services;
    private readonly IWorkflowStateSerializer _workflowStateSerializer;

    public Tests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper).WithCapturingTextWriter(_capturingTextWriter).Build();
        _workflowStateSerializer = _services.GetRequiredService<IWorkflowStateSerializer>();
    }

    [Fact(DisplayName = "User-objects containing $id don't break deserialization into ExpandoObject.")]
    public async Task Test1()
    {
        // Populate registries.
        await _services.PopulateRegistriesAsync();

        // Import workflow.
        var workflowFileName = "Scenarios/JsonObjectSerialization/Workflows/instance-serialization.json";
        var workflowDefinition = await _services.ImportWorkflowDefinitionAsync(workflowFileName);

        // Execute.
        var workflowState = await _services.RunWorkflowUntilEndAsync(workflowDefinition.DefinitionId);
        
        // Serialize workflow state.
        var serializedWorkflowState = _workflowStateSerializer.Serialize(workflowState);

        // Attempt to deserialize workflow state.
        _workflowStateSerializer.Deserialize<WorkflowState>(serializedWorkflowState);

        // If we reach this point, the test has passed. Otherwise, an exception would have been thrown.
    }
}