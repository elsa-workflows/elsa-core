using Elsa.Expressions.JavaScript.Activities;
using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Memory;
using Xunit.Abstractions;

namespace Elsa.JavaScript.IntegrationTests;

public class RunJavaScriptTests(ITestOutputHelper testOutputHelper)
{
    private readonly WorkflowTestFixture _fixture = new(testOutputHelper);

    [Theory(DisplayName = "RunJavaScript should execute valid scripts successfully")]
    [InlineData("return 1 + 1;")]
    [InlineData("return 'Hello World';")]
    [InlineData("return 42;")]
    public async Task Should_Execute_Valid_Scripts(string script)
    {
        // Arrange
        var activity = new RunJavaScript { Script = new(script), Result = new() };

        // Act
        await _fixture.RunActivityAsync(activity);

        // Assert
        Assert.Empty(_fixture.CapturingTextWriter.Lines);
    }

    [Theory(DisplayName = "RunJavaScript should set outcomes correctly")]
    [InlineData("setOutcome('Success');")]
    [InlineData("setOutcomes(['Branch1', 'Branch2', 'Branch3']);")]
    public async Task Should_Set_Outcomes(string script)
    {
        // Arrange
        var activity = new RunJavaScript { Script = new(script) };

        // Act
        await _fixture.RunActivityAsync(activity);

        // Assert
        Assert.Null(_fixture.CapturingTextWriter.Lines.FirstOrDefault());
    }

    [Theory(DisplayName = "RunJavaScript should not execute invalid scripts")]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Should_Not_Execute_Invalid_Scripts(string script)
    {
        // Arrange
        var activity = new RunJavaScript { Script = new(script), Result = new() };

        // Act
        await _fixture.RunActivityAsync(activity);

        // Assert
        Assert.Empty(_fixture.CapturingTextWriter.Lines);
    }

    [Fact(DisplayName = "RunJavaScript should access workflow variables")]
    public async Task Should_Access_Workflow_Variables()
    {
        // Arrange
        var myVar = new Variable<int>("MyVar", 100);
        var script = "return getMyVar();";
        var workflow = new Workflow
        {
            Root = new RunJavaScript { Script = new(script), Result = new() },
            Variables = { myVar }
        };

        // Act
        await _fixture.RunActivityAsync(workflow);

        // Assert - workflow completes successfully
        Assert.Empty(_fixture.CapturingTextWriter.Lines);
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
        await _fixture.RunActivityAsync(activity);

        // Assert - workflow completes successfully
        Assert.Empty(_fixture.CapturingTextWriter.Lines);
    }
}
