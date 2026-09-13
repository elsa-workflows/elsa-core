using Elsa.Testing.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.IntegrationTests.Activities;

public class FinishTests : IAsyncDisposable
{
    private readonly IWorkflowRunner _workflowRunner;
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IServiceProvider _services;

    public FinishTests()
    {
        _services = new TestApplicationBuilder(TestContext.Current!.Output.StandardOutput).WithCapturingTextWriter(_capturingTextWriter).Build();
        _workflowRunner = _services.GetRequiredService<IWorkflowRunner>();
    }

    [Test]
    [DisplayName("Subsequent activities are not executed")]
    public async Task Test1()
    {
        await _services.PopulateRegistriesAsync();
        await _workflowRunner.RunAsync<FinishSequentialWorkflow>();
        var lines = _capturingTextWriter.Lines.ToList();
        await Assert.That(lines).IsEquivalentTo(new[] { "Line 1" }, TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }
    
    [Test]
    [DisplayName("Workflow status is set to Finished")]
    public async Task Test2()
    {
        await _services.PopulateRegistriesAsync();
        var result = await _workflowRunner.RunAsync<FinishSequentialWorkflow>();
        var workflowState = result.WorkflowState;
        await Assert.That(workflowState.Status).IsEqualTo(WorkflowStatus.Finished);
        await Assert.That(workflowState.SubStatus).IsEqualTo(WorkflowSubStatus.Finished);
    }
    
    [Test]
    [DisplayName("All bookmarks are removed")]
    public async Task Test3()
    {
        await _services.PopulateRegistriesAsync();
        var result = await _workflowRunner.RunAsync<FinishSequentialWorkflow>();
        await Assert.That(result.WorkflowState.Bookmarks).IsEmpty();
    }

    public ValueTask DisposeAsync() => TestResourceDisposal.DisposeAsync(_services);
}
