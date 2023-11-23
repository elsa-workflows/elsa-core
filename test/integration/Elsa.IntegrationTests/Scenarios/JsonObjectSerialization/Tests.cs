using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using Elsa.Expressions.Contracts;
using Elsa.Testing.Shared;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Serialization.Converters;
using Elsa.Workflows.Core.Serialization.ReferenceHandlers;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.IntegrationTests.Scenarios.JsonObjectSerialization;

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
        var serializedWorkflowState = await _workflowStateSerializer.SerializeAsync(workflowState);

        // Attempt to deserialize workflow state.
        await _workflowStateSerializer.DeserializeAsync<WorkflowState>(serializedWorkflowState);

        // If we reach this point, the test has passed. Otherwise, an exception would have been thrown.
    }
}