using System;
using System.Linq;
using System.Threading.Tasks;
using Elsa.Testing.Shared;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.IntegrationTests.Scenarios.DependencyWorkflows;

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
    public async Task Test1()
    {
        // Populate registries.
        await _services.PopulateRegistriesAsync();

        // Run the "matter" workflow.
        await _services.RunWorkflowUntilEndAsync("matter-workflow");

        // Assert.
        var lines = _capturingTextWriter.Lines.ToList();

        Assert.Equal(new[] { "Atom", "Atom", "Atom", "Atom", "Atom", "Atom", "Atom", "Atom", "Atom" }, lines);
    }
}