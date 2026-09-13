using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.Options;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.IntegrationTests.Scenarios.Blocking;

public class Tests : IAsyncDisposable
{
    private readonly IWorkflowRunner _workflowRunner;
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IWorkflowBuilderFactory _workflowBuilderFactory;
    private readonly IServiceProvider _services;

    public Tests()
    {
        _services = new TestApplicationBuilder(TestContext.Current!.Output.StandardOutput).WithCapturingTextWriter(_capturingTextWriter).Build();
        _workflowBuilderFactory = _services.GetRequiredService<IWorkflowBuilderFactory>();
        _workflowRunner = _services.GetRequiredService<IWorkflowRunner>();
    }

    [Test]
    [DisplayName("Subsequent activity does not get scheduled when previous activity created a bookmark")]
    public async Task Test1()
    {
        await _services.PopulateRegistriesAsync();
        await _workflowRunner.RunAsync<BlockingSequentialWorkflow>();
        var lines = _capturingTextWriter.Lines.ToList();
        await Assert.That(lines).IsEquivalentTo(new[] { "Line 1" }, TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    [DisplayName("Subsequent activities are scheduled when resuming workflow using bookmark")]
    public async Task Test2()
    {
        await _services.PopulateRegistriesAsync();
        var workflow = await _workflowBuilderFactory.CreateBuilder().BuildWorkflowAsync<BlockingSequentialWorkflow>();

        // Start workflow.
        var result = await _workflowRunner.RunAsync(workflow);
        var bookmark = result.WorkflowState.Bookmarks.FirstOrDefault(x => x.ActivityId == "Resume");

        // Resume workflow.
        var runOptions = new RunWorkflowOptions { BookmarkId = bookmark!.Id };
        await _workflowRunner.RunAsync(workflow, result.WorkflowState, runOptions);

        // Verify expected output.
        var lines = _capturingTextWriter.Lines.ToList();
        await Assert.That(lines).IsEquivalentTo(new[] { "Line 1", "Line 2", "Line 3" }, TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    public ValueTask DisposeAsync() => TestResourceDisposal.DisposeAsync(_services);
}
