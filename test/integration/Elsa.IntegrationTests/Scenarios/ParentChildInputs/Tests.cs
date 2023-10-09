using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elsa.Testing.Shared;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.IntegrationTests.Scenarios.ParentChildInputs;

/// <summary>
/// Tests for the flowchart completion feature using various workflow setups.
/// </summary>
public class Tests
{
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IServiceProvider _services;

    public Tests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper).WithCapturingTextWriter(_capturingTextWriter).Build();
    }

    [Fact(DisplayName = "Child activity receives workflow input from parent event if same name.")]
    public async Task Test1()
    {
        // Populate registries.
        await _services.PopulateRegistriesAsync();

        // Import child workflow.
        var childFileName = $"Scenarios/ParentChildInputs/Workflows/child.json";
        await _services.ImportWorkflowDefinitionAsync(childFileName);

        // Import parent workflow.
        var parentFileName = $"Scenarios/ParentChildInputs/Workflows/parent1.json";
        var workflowDefinition = await _services.ImportWorkflowDefinitionAsync(parentFileName);

        // Execute.
        var input = new Dictionary<string, object> { ["Input1"] = "Foo" };
        await _services.RunWorkflowUntilEndAsync(workflowDefinition.DefinitionId, input);

        // Assert expected output.
        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Equal(new[] { "Parent: Foo", "Child: Bar" }, lines);
    }
    
    [Fact(DisplayName = "Child activity receives input from parent event if same name and does not use global workflow input.")]
    public async Task Test2()
    {
        // Populate registries.
        await _services.PopulateRegistriesAsync();

        // Import child workflow.
        var childFileName = $"Scenarios/ParentChildInputs/Workflows/child.json";
        await _services.ImportWorkflowDefinitionAsync(childFileName);

        // Import parent workflow.
        var parentFileName = $"Scenarios/ParentChildInputs/Workflows/parent2.json";
        var workflowDefinition = await _services.ImportWorkflowDefinitionAsync(parentFileName);

        // Execute.
        var input = new Dictionary<string, object> { ["Input1"] = "Foo" };
        await _services.RunWorkflowUntilEndAsync(workflowDefinition.DefinitionId, input);

        // Assert expected output.
        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Equal(new[] { "Parent: Foo", "Child: Foo" }, lines);
    }
}