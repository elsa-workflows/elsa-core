using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Evaluation;

/// <summary>
/// Reproduces https://github.com/elsa-workflows/elsa-core/issues/9235:
/// reading another activity's output from code must return the value recorded in the reader's own branch or iteration,
/// also when iterations run concurrently.
/// </summary>
public class ActivityResultEvaluationTests(ITestOutputHelper testOutputHelper)
{
    private const string ProducerName = "Producer";

    [Theory(DisplayName = "An output read inside a ParallelForEach iteration returns the value produced in that iteration")]
    [InlineData(OutputAccessor.ActivityGetResult)]
    [InlineData(OutputAccessor.ActivityExecutionContextGetResult)]
    [InlineData(OutputAccessor.ExpressionExecutionContextGetOutput)]
    public async Task OutputReadInParallelIterationReturnsThatIterationsValue(OutputAccessor accessor)
    {
        var lines = await RunAsync(new ParallelProducerWorkflow(accessor, current => current));

        Assert.Equal(
        [
            "CurrentValue = 1, Producer = 1",
            "CurrentValue = 2, Producer = 2",
            "CurrentValue = 3, Producer = 3"
        ], lines);
    }

    [Theory(DisplayName = "A null output in an iteration is not replaced by another iteration's output")]
    [InlineData(OutputAccessor.ActivityGetResult)]
    [InlineData(OutputAccessor.ExpressionExecutionContextGetOutput)]
    public async Task NullOutputInIterationIsReturnedAsNull(OutputAccessor accessor)
    {
        var lines = await RunAsync(new ParallelProducerWorkflow(accessor, current => current == 2 ? null : current));

        Assert.Equal(
        [
            "CurrentValue = 1, Producer = 1",
            "CurrentValue = 2, Producer = ",
            "CurrentValue = 3, Producer = 3"
        ], lines);
    }

    [Fact(DisplayName = "A container reading its child's output from its own callback returns the value produced in its own iteration")]
    public async Task ContainerReadingChildOutputReturnsItsOwnIterationsValue()
    {
        var lines = await RunAsync(new ContainerReadsChildWorkflow());

        Assert.Equal(
        [
            "CurrentValue = 1, Producer = 1",
            "CurrentValue = 2, Producer = 2",
            "CurrentValue = 3, Producer = 3"
        ], lines);
    }

    [Fact(DisplayName = "An inner activity's output read from code after its loop completed returns the most recent value")]
    public async Task OutputReadAfterLoopReturnsMostRecentValue()
    {
        var lines = await RunAsync(new ReadAfterLoopWorkflow());

        Assert.Equal(["After the loop, Producer = 3"], lines);
    }

    public enum OutputAccessor
    {
        ActivityGetResult,
        ActivityExecutionContextGetResult,
        ExpressionExecutionContextGetOutput
    }

    private async Task<List<string>> RunAsync(IWorkflow workflow)
    {
        var fixture = new WorkflowTestFixture(testOutputHelper);
        await fixture.BuildAsync();

        var result = await fixture.RunWorkflowAsync(workflow);

        Assert.Equal(WorkflowStatus.Finished, result.WorkflowState.Status);
        return fixture.CapturingTextWriter.Lines.ToList();
    }

    /// <summary>
    /// ParallelForEach over [1, 2, 3] whose body is a Sequence of Producer (returns a value derived from CurrentValue) and WriteLine (reads the output of Producer).
    /// The default scheduler runs all three Producers before any WriteLine.
    /// </summary>
    private class ParallelProducerWorkflow(OutputAccessor accessor, Func<int, int?> produce) : WorkflowBase
    {
        protected override void Build(IWorkflowBuilder builder)
        {
            var producer = new Inline<int?>(context => produce(context.GetVariable<int>("CurrentValue"))) { Name = ProducerName };

            builder.Root = new ParallelForEach<int>([1, 2, 3])
            {
                Body = new Sequence
                {
                    Activities =
                    {
                        producer,
                        new WriteLine(context => $"CurrentValue = {context.GetVariable<int>("CurrentValue")}, Producer = {ReadProducer(context, producer)}")
                    }
                }
            };
        }

        private object? ReadProducer(ExpressionExecutionContext context, IActivity producer) => accessor switch
        {
            OutputAccessor.ActivityGetResult => context.GetActivityExecutionContext().FindActivityByIdOrName(ProducerName)!.GetResult(context.GetActivityExecutionContext()),
            OutputAccessor.ActivityExecutionContextGetResult => context.GetActivityExecutionContext().GetResult(producer),
            OutputAccessor.ExpressionExecutionContextGetOutput => context.GetOutput(ProducerName, null),
            _ => throw new ArgumentOutOfRangeException(nameof(accessor))
        };
    }

    /// <summary>
    /// ParallelForEach over [1, 2, 3] whose body is Producer, followed by a WriteLine outside the loop that reads the output of Producer.
    /// </summary>
    private class ReadAfterLoopWorkflow : WorkflowBase
    {
        protected override void Build(IWorkflowBuilder builder)
        {
            var producer = new Inline<int>(context => context.GetVariable<int>("CurrentValue")) { Name = ProducerName };

            builder.Root = new Sequence
            {
                Activities =
                {
                    new ParallelForEach<int>([1, 2, 3]) { Body = producer },
                    new WriteLine(context => $"After the loop, Producer = {producer.GetResult(context)}")
                }
            };
        }
    }

    /// <summary>
    /// ParallelForEach over [1, 2, 3] whose body is a container that reads the output of its own child Producer.
    /// </summary>
    private class ContainerReadsChildWorkflow : WorkflowBase
    {
        protected override void Build(IWorkflowBuilder builder)
        {
            builder.Root = new ParallelForEach<int>([1, 2, 3])
            {
                Body = new ReadChildOutputContainer
                {
                    Producer = new Inline<int>(context => context.GetVariable<int>("CurrentValue")) { Name = ProducerName },
                    Step = new Inline(() => { })
                }
            };
        }
    }

    /// <summary>
    /// Runs Producer, then Step, and then reads the output of Producer from its own callback.
    /// Step makes the iterations interleave, so every Producer has completed before the first container reads its output.
    /// </summary>
    private class ReadChildOutputContainer : Activity
    {
        [Port] public IActivity Producer { get; set; } = null!;
        [Port] public IActivity Step { get; set; } = null!;

        protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
        {
            await context.ScheduleActivityAsync(Producer, OnProducerCompletedAsync);
        }

        private async ValueTask OnProducerCompletedAsync(ActivityCompletedContext context)
        {
            await context.TargetContext.ScheduleActivityAsync(Step, OnStepCompletedAsync);
        }

        private async ValueTask OnStepCompletedAsync(ActivityCompletedContext context)
        {
            var containerContext = context.TargetContext;
            var currentValue = containerContext.ExpressionExecutionContext.GetVariable<int>("CurrentValue");
            var producerValue = containerContext.GetResult(Producer);
            await context.GetRequiredService<IStandardOutStreamProvider>().GetTextWriter().WriteLineAsync($"CurrentValue = {currentValue}, Producer = {producerValue}");
            await containerContext.CompleteActivityAsync();
        }
    }
}
