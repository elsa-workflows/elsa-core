using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;

namespace Elsa.Workflows.IntegrationTests.Activities;

public class IfTests : IAsyncDisposable
{
    private readonly WorkflowTestFixture _fixture = new(TestContext.Current!.Output.StandardOutput);

    [Test]
    [DisplayName("The correct branch executes when condition is true: $conditionResult")]
    [Arguments(true)]
    [Arguments(false)]
    public async Task Test1(bool conditionResult)
    {
        var result = default(bool?);
        
        var activity = new If(() => conditionResult)
        {
            Then = new Inline(() => result = true),
            Else = new Inline(() => result = false)
        };
        await _fixture.RunActivityAsync(activity);
        await Assert.That(result).IsEqualTo(conditionResult);
    }

    [Test]
    [DisplayName("The If activity completes only after either one of its branches completed: $conditionResult")]
    [Arguments(true)]
    [Arguments(false)]
    public async Task Test2(bool conditionResult)
    {
        var activity = new If(() => conditionResult)
        {
            Then = new Inline(),
            Else = new Inline()
        };
        var result = await _fixture.RunActivityAsync(activity);
        await Assert.That(result.WorkflowState.Status).IsEqualTo(WorkflowStatus.Finished);
    }
    
    [Test]
    [DisplayName("The If activity produces a result when one of its branches completes")]
    public async Task Test3()
    {
        var activity = new If(() => true);
        var result = await _fixture.RunActivityAsync(activity);
        var activityResult = result.GetActivityOutput<bool>(activity);
        
        await Assert.That(activityResult).IsTrue();
    }

    public ValueTask DisposeAsync() => TestResourceDisposal.DisposeAsync(_fixture);
}
