using Elsa.Testing.Shared;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Scenarios.DependencyWorkflows;

public class Tests
{
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IServiceProvider _services;

    public Tests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper)
            .WithCapturingTextWriter(_capturingTextWriter)
            .WithWorkflowsFromDirectory("Scenarios", "DependencyWorkflows", "Workflows")
            .Build();
    }

    [Fact(DisplayName = "Workflows provided from JSON files that depend on other workflows are executed correctly.")]
    public async Task Should_Execute_Multi_Level_Nested_Workflow_Definitions()
    {
        // Populate registries.
        await _services.PopulateRegistriesAsync();

        // Run the "matter" workflow which contains MoleculeActivity, which contains AtomActivity.
        // Matter -> 3x Molecule -> each Molecule contains 3x Atom = 9 "Atom" outputs total
        await _services.RunWorkflowUntilEndAsync("matter-workflow");

        // Assert.
        var lines = _capturingTextWriter.Lines.ToList();

        Assert.Equal(new[] { "Atom", "Atom", "Atom", "Atom", "Atom", "Atom", "Atom", "Atom", "Atom" }, lines);
    }
}