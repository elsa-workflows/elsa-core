using System;
using System.Linq;
using System.Threading.Tasks;
using Elsa.Testing.Shared;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.IntegrationTests.Scenarios.ParentChildOutputMapping;

/// <summary>
/// Tests for mapping an activity's output directly to the workflow's output definition.
/// </summary>
public class Tests
{
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IServiceProvider _services;

    public Tests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper).WithCapturingTextWriter(_capturingTextWriter).Build();
    }

    [Fact(DisplayName = "Parent workflow receives output from child workflow.")]
    public async Task Test1()
    {
        // Populate registries.
        await _services.PopulateRegistriesAsync();

        // Import child workflow.
        var sumFileName = "Scenarios/ParentChildOutputMapping/Workflows/sum.json";
        await _services.ImportWorkflowDefinitionAsync(sumFileName);

        // Import parent workflow.
        var calculatorFileName = "Scenarios/ParentChildOutputMapping/Workflows/calculator.json";
        var workflowDefinition = await _services.ImportWorkflowDefinitionAsync(calculatorFileName);

        // Execute.
        await _services.RunWorkflowUntilEndAsync(workflowDefinition.DefinitionId);

        // Assert expected output.
        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Equal(new[] { "Result: 9" }, lines);
    }
}