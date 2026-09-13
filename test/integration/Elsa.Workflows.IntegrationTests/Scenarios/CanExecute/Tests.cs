using Elsa.Testing.Shared;
using Elsa.Workflows.IntegrationTests.Scenarios.CanExecute.Activities;
using Elsa.Workflows.IntegrationTests.Scenarios.CanExecute.Workflows;
using Elsa.Workflows.Options;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.IntegrationTests.Scenarios.CanExecute;

public class CanExecuteTests : IAsyncDisposable
{
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IServiceProvider _services;
    private readonly IWorkflowRunner _workflowRunner;

    public CanExecuteTests()
    {
        _services = new TestApplicationBuilder(TestContext.Current!.Output.StandardOutput)
            .WithCapturingTextWriter(_capturingTextWriter)
            .AddActivitiesFrom<CustomActivity>()
            .Build();

        _workflowRunner = _services.GetRequiredService<IWorkflowRunner>();
    }

    [Test]
    [DisplayName("Activities are executed only when they report that they can execute: $magicNumber")]
    [Arguments(12, "Magic number is 12")]
    [Arguments(42, "Magic number is 42\nWelcome to the world of Might and Magic!\nDone")]
    public async Task Test1(int magicNumber, string expectedLines)
    {
        await _services.PopulateRegistriesAsync();
        var runOptions = new RunWorkflowOptions
        {
            Input = new Dictionary<string, object>
            {
                ["MagicNumber"] = magicNumber
            }
        };
        await _workflowRunner.RunAsync<MagicWorkflow>(runOptions);
        var lines = _capturingTextWriter.Lines.ToList();
        await Assert.That(lines).IsEquivalentTo(expectedLines.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries), TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    public ValueTask DisposeAsync() => TestResourceDisposal.DisposeAsync(_services);
}
