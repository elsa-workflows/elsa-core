using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.Options;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Scenarios.NamedVariablePersistence;

public class Tests
{
    private readonly IWorkflowRunner _workflowRunner;
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IWorkflowBuilderFactory _workflowBuilderFactory;
    private readonly IServiceProvider _services;

    public Tests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper).WithCapturingTextWriter(_capturingTextWriter).Build();
        _workflowBuilderFactory = _services.GetRequiredService<IWorkflowBuilderFactory>();
        _workflowRunner = _services.GetRequiredService<IWorkflowRunner>();
    }

    [Fact(DisplayName = "Named WithVariable value survives suspend and resume")]
    public async Task NamedWithVariable_ValueSurvivesSuspendAndResume()
    {
        // Arrange
        await _services.PopulateRegistriesAsync();
        var workflow = await _workflowBuilderFactory.CreateBuilder().BuildWorkflowAsync<NamedVariableSurvivesSuspendWorkflow>();

        // Act
        var started = await _workflowRunner.RunAsync(workflow);
        var bookmark = started.WorkflowState.Bookmarks.Single(x => x.ActivityId == "Resume");
        var runOptions = new RunWorkflowOptions { BookmarkId = bookmark.Id };
        var resumed = await _workflowRunner.RunAsync(workflow, started.WorkflowState, runOptions);

        // Assert
        Assert.Equal(WorkflowStatus.Running, started.WorkflowState.Status);
        Assert.Equal(WorkflowSubStatus.Suspended, started.WorkflowState.SubStatus);
        Assert.Equal(WorkflowStatus.Finished, resumed.WorkflowState.Status);
        Assert.Equal(
            [
                "before suspend: hello",
                "after resume: hello"
            ],
            _capturingTextWriter.Lines.ToList());
    }
}
