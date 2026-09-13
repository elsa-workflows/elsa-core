using Elsa.Expressions.JavaScript.Activities;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Memory;

namespace Elsa.JavaScript.IntegrationTests;

public class RunJavaScriptTests : IAsyncDisposable
{
    private readonly WorkflowTestFixture _fixture = new(TestContext.Current!.Output.StandardOutput);

    public ValueTask DisposeAsync() => _fixture.DisposeAsync();

    [Test]
    [DisplayName("RunJavaScript should execute valid scripts successfully: $script")]
    [Arguments("return 1 + 1;", 2d)]
    [Arguments("return 'Hello World';", "Hello World")]
    [Arguments("return 42;", 42d)]
    public async Task Should_Execute_Valid_Scripts(string script, object expectedOutput)
    {
        // Arrange
        var activity = new RunJavaScript { Script = new(script), Result = new() };

        // Act
        var result = await _fixture.RunActivityAsync(activity);

        // Assert - script returns expected value
        var output = result.GetActivityOutput<object>(activity);
        await Assert.That(output).IsEqualTo(expectedOutput);
    }

    [Test]
    [DisplayName("RunJavaScript should set outcomes correctly: $script")]
    [MethodDataSource(nameof(OutcomeTestCases))]
    public async Task Should_Set_Outcomes(string script, string[] expectedOutcomes)
    {
        // Arrange
        var activity = new RunJavaScript { Script = new(script) };

        // Act
        var result = await _fixture.RunActivityAsync(activity);

        // Assert - activity produced expected outcomes
        var outcomes = _fixture.GetOutcomes(result, activity).ToArray();
        await Assert.That(outcomes.Length).IsEqualTo(expectedOutcomes.Length);
        foreach (var expectedOutcome in expectedOutcomes)
        {
            await Assert.That(outcomes).Contains(expectedOutcome);
        }
    }

    [Test]
    [DisplayName("RunJavaScript should produce null output for empty or whitespace scripts: $script")]
    [Arguments("", DisplayName = "RunJavaScript should produce null output for empty or whitespace scripts: empty")]
    [Arguments("   ", DisplayName = "RunJavaScript should produce null output for empty or whitespace scripts: whitespace")]
    public async Task Should_Produce_Null_Output_For_Empty_Scripts(string script)
    {
        // Arrange
        var activity = new RunJavaScript { Script = new(script), Result = new() };

        // Act
        var result = await _fixture.RunActivityAsync(activity);

        // Assert - empty/whitespace scripts produce no output
        var output = result.GetActivityOutput<object>(activity);
        await Assert.That(output).IsNull();
    }

    [Test]
    [DisplayName("RunJavaScript should access workflow variables")]
    public async Task Should_Access_Workflow_Variables()
    {
        // Arrange
        var myVar = new Variable<int>("MyVar", 100);
        var script = "return getMyVar();";
        var runJavaScript = new RunJavaScript { Script = new(script), Result = new() };
        var workflow = new Workflow
        {
            Root = runJavaScript,
            Variables = { myVar }
        };

        // Act
        var result = await _fixture.RunActivityAsync(workflow);

        // Assert - variable was accessed and returned
        var output = result.GetActivityOutput<int>(runJavaScript);
        await Assert.That(output).IsEqualTo(100);
    }

    [Test]
    [DisplayName("RunJavaScript should execute complex script with multiple statements and outcomes")]
    public async Task Should_Execute_Complex_Script_With_Multiple_Statements()
    {
        // Arrange
        var script = @"
            var x = 10;
            var y = 20;
            var sum = x + y;
            if (sum > 25) {
                setOutcome('Large');
            } else {
                setOutcome('Small');
            }
            return sum;
        ";
        var activity = new RunJavaScript { Script = new(script), Result = new() };

        // Act
        var result = await _fixture.RunActivityAsync(activity);

        // Assert - script returns calculated sum
        var output = result.GetActivityOutput<int>(activity);
        await Assert.That(output).IsEqualTo(30);
    }

    [Test]
    [DisplayName("RunJavaScript should fault on invalid JavaScript syntax: $script")]
    [Arguments("this is not valid javascript")]
    [Arguments("return unclosedBracket(;")]
    [Arguments("undefined.property.access")]
    public async Task Should_Fault_On_Invalid_JavaScript(string script)
    {
        // Arrange
        var activity = new RunJavaScript { Script = new(script), Result = new() };

        // Act
        var result = await _fixture.RunActivityAsync(activity);

        // Assert - activity should be in faulted state
        var activityStatus = _fixture.GetActivityStatus(result, activity);
        await Assert.That(activityStatus).IsEqualTo(ActivityStatus.Faulted);
    }

    public static IEnumerable<TestDataRow<Func<(string Script, string[] ExpectedOutcomes)>>> OutcomeTestCases()
    {
        yield return new(static () => ("setOutcome('Success');", ["Success"]));
        yield return new(static () => ("setOutcomes(['Branch1', 'Branch2', 'Branch3']);", ["Branch1", "Branch2", "Branch3"]));
    }
}
