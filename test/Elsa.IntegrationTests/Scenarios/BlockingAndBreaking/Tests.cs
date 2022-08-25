using System.Linq;
using System.Threading.Tasks;
using Elsa.Testing.Shared;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.IntegrationTests.Scenarios.BlockingAndBreaking;

public class Tests
{
    private readonly IWorkflowRunner _workflowRunner;
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IWorkflowBuilderFactory _workflowBuilderFactory;

    public Tests(ITestOutputHelper testOutputHelper)
    {
        var services = new TestApplicationBuilder(testOutputHelper).WithCapturingTextWriter(_capturingTextWriter).Build();
        _workflowBuilderFactory = services.GetRequiredService<IWorkflowBuilderFactory>();
        _workflowRunner = services.GetRequiredService<IWorkflowRunner>();
    }

    [Fact(DisplayName = "While loop blocks when bookmark is created")]
    public async Task Test1()
    {
        var workflow = await _workflowBuilderFactory.CreateBuilder().BuildWorkflowAsync<BreakWhileBlockForkWorkflow>();
        
        // Start workflow.
        var result1 = await _workflowRunner.RunAsync(workflow);

        // Resume one of the branches.
        var bookmark = result1.Bookmarks.FirstOrDefault(x => x.ActivityId == "Branch 1");
        var result2 = await _workflowRunner.RunAsync(workflow, result1.WorkflowState, bookmark);
        
        // Assert that all bookmarks have been cleared.
        Assert.Empty(result2.Bookmarks);
        
        // Verify expected output.
        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Equal(new[] { "Branch 1", "Branch 2", "Branch 1 - Resumed" }, lines);
    }
}