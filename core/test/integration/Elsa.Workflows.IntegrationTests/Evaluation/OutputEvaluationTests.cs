using Elsa.Expressions.JavaScript.Models;
using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Activities.Flowchart.Activities;
using Elsa.Workflows.Activities.Flowchart.Extensions;
using Elsa.Workflows.Activities.Flowchart.Models;
using Elsa.Workflows.Models;
using Elsa.Workflows.Options;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Evaluation;

/// <summary>
/// Reproduces https://github.com/elsa-workflows/elsa-core/issues/9233:
/// reading an activity's output from an expression must return the most recent in-scope value, also when the activity runs several times in the same container.
/// </summary>
public class OutputEvaluationTests(ITestOutputHelper testOutputHelper)
{
    private static readonly string[] ExpectedLines =
    [
        "Producer = 1, Counter = 1",
        "Producer = 2, Counter = 2",
        "Producer = 3, Counter = 3"
    ];

    [Theory(DisplayName = "An output read from an expression returns the latest value when the activity runs in a loop")]
    [InlineData(OutputAccessor.CSharpDelegate, FlowchartExecutionMode.CounterBased)]
    [InlineData(OutputAccessor.CSharpDelegate, FlowchartExecutionMode.TokenBased)]
    [InlineData(OutputAccessor.JavaScript, FlowchartExecutionMode.CounterBased)]
    [InlineData(OutputAccessor.JavaScript, FlowchartExecutionMode.TokenBased)]
    public async Task OutputReadFromExpressionReturnsLatestValueInLoop(OutputAccessor accessor, FlowchartExecutionMode executionMode)
    {
        // Arrange
        var fixture = new WorkflowTestFixture(testOutputHelper).ConfigureElsa(elsa => elsa.UseJavaScript());
        await fixture.BuildAsync();
        var workflow = new LoopingProducerWorkflow(accessor);
        var options = new RunWorkflowOptions().WithFlowchartExecutionMode(executionMode);

        // Act
        var result = await fixture.RunWorkflowAsync(workflow, options);

        // Assert
        Assert.Equal(WorkflowStatus.Finished, result.WorkflowState.Status);
        Assert.Equal(ExpectedLines, fixture.CapturingTextWriter.Lines.ToList());
    }

    public enum OutputAccessor
    {
        CSharpDelegate,
        JavaScript
    }

    /// <summary>
    /// SetVariable (Counter = Counter + 1) -> Producer (returns Counter) -> WriteLine (reads the output of Producer) -> FlowDecision (Counter &lt; 3) -> back to SetVariable.
    /// Every activity is a direct child of the same flowchart, so every Producer record has the same container.
    /// </summary>
    private class LoopingProducerWorkflow(OutputAccessor accessor) : WorkflowBase
    {
        protected override void Build(IWorkflowBuilder builder)
        {
            var counter = builder.WithVariable<int>("Counter", 0);
            var increment = new SetVariable<int>(counter, context => counter.Get(context) + 1);
            var producer = new Inline<int>(context => counter.Get(context)) { Name = "Producer" };
            var writeLine = accessor == OutputAccessor.CSharpDelegate
                ? new WriteLine(new Input<string>(context => $"Producer = {context.GetOutput("Producer", null)}, Counter = {counter.Get(context)}"))
                : new WriteLine(JavaScriptExpression.Create("`Producer = ${getOutputFrom('Producer')}, Counter = ${getCounter()}`"));
            var decision = new FlowDecision(context => counter.Get(context) < 3);

            builder.Root = new Flowchart
            {
                Start = increment,
                Activities = { increment, producer, writeLine, decision },
                Connections =
                {
                    new(increment, producer),
                    new(producer, writeLine),
                    new(writeLine, decision),
                    new(new(decision, "True"), new Endpoint(increment))
                }
            };
        }
    }
}
