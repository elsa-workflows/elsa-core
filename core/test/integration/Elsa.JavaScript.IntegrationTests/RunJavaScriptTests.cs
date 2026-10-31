using Elsa.Expressions.JavaScript.Activities;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Memory;
using Xunit.Abstractions;

namespace Elsa.JavaScript.IntegrationTests;

public class RunJavaScriptTests(ITestOutputHelper testOutputHelper)
{
    private readonly WorkflowTestFixture _fixture = new(testOutputHelper);

    [Theory(DisplayName = "RunJavaScript should execute valid scripts successfully")]
    [InlineData("return 1 + 1;", 2d)]
    [InlineData("return 'Hello World';", "Hello World")]
    [InlineData("return 42;", 42d)]
    public async Task Should_Execute_Valid_Scripts(string script, object expectedOutput)
    {
        // Arrange
        var activity = new RunJavaScript { Script = new(script), Result = new() };

        // Act
        var result = await _fixture.RunActivityAsync(activity);

        // Assert - script returns expected value
        var output = result.GetActivityOutput<object>(activity);
        Assert.Equal(expectedOutput, output);
    }

    [Theory(DisplayName = "RunJavaScript should set outcomes correctly")]
    [InlineData("setOutcome('Success');", new[] { "Success" })]
    [InlineData("setOutcomes(['Branch1', 'Branch2', 'Branch3']);", new[] { "Branch1", "Branch2", "Branch3" })]
    public async Task Should_Set_Outcomes(string script, string[] expectedOutcomes)
    {
        // Arrange
        var activity = new RunJavaScript { Script = new(script) };

        // Act
        var result = await _fixture.RunActivityAsync(activity);

        // Assert - activity produced expected outcomes
        var outcomes = _fixture.GetOutcomes(result, activity).ToArray();
        Assert.Equal(expectedOutcomes.Length, outcomes.Length);
        foreach (var expectedOutcome in expectedOutcomes)
        {
            Assert.Contains(expectedOutcome, outcomes);
        }
    }

    [Theory(DisplayName = "RunJavaScript should produce null output for empty or whitespace scripts")]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Should_Produce_Null_Output_For_Empty_Scripts(string script)
    {
        // Arrange
        var activity = new RunJavaScript { Script = new(script), Result = new() };

        // Act
        var result = await _fixture.RunActivityAsync(activity);

        // Assert - empty/whitespace scripts produce no output
        var output = result.GetActivityOutput<object>(activity);
        Assert.Null(output);
    }

    [Fact(DisplayName = "RunJavaScript should access workflow variables")]
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
        Assert.Equal(100, output);
    }

    [Fact(DisplayName = "RunJavaScript should execute complex script with multiple statements and outcomes")]
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
        Assert.Equal(30, output);
    }

    [Theory(DisplayName = "RunJavaScript should fault on invalid JavaScript syntax")]
    [InlineData("this is not valid javascript")]
    [InlineData("return unclosedBracket(;")]
    [InlineData("undefined.property.access")]
    public async Task Should_Fault_On_Invalid_JavaScript(string script)
    {
        // Arrange
        var activity = new RunJavaScript { Script = new(script), Result = new() };

        // Act
        var result = await _fixture.RunActivityAsync(activity);

        // Assert - activity should be in faulted state
        var activityStatus = _fixture.GetActivityStatus(result, activity);
        Assert.Equal(ActivityStatus.Faulted, activityStatus);
    }
}
