using System;
using Elsa.Testing.Shared;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.IntegrationTests.Scenarios.ImportAndExecute;

public class Tests
{
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IServiceProvider _services;

    public Tests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper)
            .WithCapturingTextWriter(_capturingTextWriter)
            .Build();
    }

    [Theory(DisplayName = "Workflow imported from file should execute successfully.")]
    [MemberData(nameof(GetSpecimen))]
    public async Task Test1(string workflowFileName, string[] expectedOutput)
    {
        // Populate registries.
        await _services.PopulateRegistriesAsync();

        // Import workflow.
        var workflowDefinition = await _services.ImportWorkflowDefinitionAsync($"Scenarios/ImportAndExecute/Workflows/{workflowFileName}");

        // Execute.
        await _services.RunWorkflowUntilEndAsync(workflowDefinition.DefinitionId);

        // Assert.
        var lines = _capturingTextWriter.Lines.ToList();

        Assert.Equal(expectedOutput, lines);
    }
    
    public static TheoryData GetSpecimen()
    {
        return new TheoryData<string, string[]>
        {
            { "writeline.json", new[] { "Dummy Text" } },
            { "implicit-loop.json", new[] { "Do something", "Retry", "Do something", "Done" } }
        };
    }
}