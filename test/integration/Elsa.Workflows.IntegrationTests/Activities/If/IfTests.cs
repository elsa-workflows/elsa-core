using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Microsoft.Extensions.DependencyInjection;
using Parlot.Fluent;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Activities;

public class IfTests
{
    private readonly IWorkflowRunner _workflowRunner;
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IServiceProvider _services;

    public IfTests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper).WithCapturingTextWriter(_capturingTextWriter).Build();
        _workflowRunner = _services.GetRequiredService<IWorkflowRunner>();
    }

    [Theory(DisplayName = "The correct branch executes when condition is true")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Test1(bool conditionResult)
    {
        var result = default(bool?);
        
        var activity = new If(() => conditionResult)
        {
            Then = new Inline(() => result = true),
            Else = new Inline(() => result = false)
        };
        await _services.RunActivityAsync(activity);
        Assert.Equal(conditionResult, result);
    }

    [Theory(DisplayName = "The If activity completes only after either one of its branches completed")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Test2(bool conditionResult)
    {
        var activity = new If(() => conditionResult)
        {
            Then = new Inline(),
            Else = new Inline()
        };
        var result = await _services.RunActivityAsync(activity);
        Assert.Equal(WorkflowStatus.Finished, result.WorkflowState.Status);
    }
    
    [Fact(DisplayName = "The If activity produces a result when one of its branches completes")]
    public async Task Test3()
    {
        var activity = new If(() => true);
        var result = await _services.RunActivityAsync(activity);
        var activityResult = result.GetActivityOutput<bool>(activity);
        
        Assert.True(activityResult);
    }
}