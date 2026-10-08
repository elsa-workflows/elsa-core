using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Activities;

public class IfTests(ITestOutputHelper testOutputHelper)
{
    private readonly WorkflowTestFixture _fixture = new(testOutputHelper);

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
        await _fixture.RunActivityAsync(activity);
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
        var result = await _fixture.RunActivityAsync(activity);
        Assert.Equal(WorkflowStatus.Finished, result.WorkflowState.Status);
    }
    
    [Fact(DisplayName = "The If activity produces a result when one of its branches completes")]
    public async Task Test3()
    {
        var activity = new If(() => true);
        var result = await _fixture.RunActivityAsync(activity);
        var activityResult = result.GetActivityOutput<bool>(activity);
        
        Assert.True(activityResult);
    }
}