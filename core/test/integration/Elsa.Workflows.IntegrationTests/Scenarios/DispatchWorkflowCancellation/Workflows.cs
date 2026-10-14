using Elsa.Workflows.Activities;
using Elsa.Workflows.Runtime.Activities;

namespace Elsa.Workflows.IntegrationTests.Scenarios.DispatchWorkflowCancellation;

/// <summary>A child workflow that blocks on an event nobody publishes, so it stays suspended until something cancels it.</summary>
public class BlockingChildWorkflow : WorkflowBase
{
    public const string BlockingActivityId = "wait-forever";

    protected override void Build(IWorkflowBuilder builder)
    {
        builder.Root = new Sequence
        {
            Activities =
            {
                new WriteLine("Child started"),
                new Event("NeverPublished") { Id = BlockingActivityId }
            }
        };
    }
}

/// <summary>The same blocking child under its own definition, dispatched fire-and-forget so a test can tell it apart.</summary>
public class FireAndForgetChildWorkflow : BlockingChildWorkflow;

/// <summary>
/// A child that dispatches a <see cref="FireAndForgetChildWorkflow"/> of its own and then finishes when the test resumes it.
/// </summary>
public class FinishingChildWorkflow : WorkflowBase
{
    public const string FinishActivityId = "finish";

    protected override void Build(IWorkflowBuilder builder)
    {
        builder.Root = new Sequence
        {
            Activities =
            {
                new DispatchWorkflow { WorkflowDefinitionId = new(nameof(FireAndForgetChildWorkflow)) },
                new Event("FinishChild") { Id = FinishActivityId }
            }
        };
    }
}

/// <summary>
/// A parent that dispatches <see cref="BlockingChildWorkflow"/> and waits for it on one fork branch, and blocks on a
/// second branch the test resumes. The fork waits for any branch, so resuming the second one completes the fork and
/// cancels the still-waiting <see cref="DispatchWorkflow"/> activity.
/// </summary>
public class DispatchAndWaitInForkWorkflow : WorkflowBase
{
    public const string DispatchActivityId = "dispatch-child";
    public const string ProceedActivityId = "proceed";

    protected override void Build(IWorkflowBuilder builder) => builder.Root = CreateFork(nameof(BlockingChildWorkflow));

    /// <summary>The fork that waits for the specified child on one branch and for the test on the other.</summary>
    public static Fork CreateFork(string childDefinitionId) => new()
    {
        JoinMode = ForkJoinMode.WaitAny,
        Branches =
        {
            new DispatchWorkflow
            {
                Id = DispatchActivityId,
                WorkflowDefinitionId = new(childDefinitionId),
                WaitForCompletion = new(true)
            },
            new Event("ProceedParent") { Id = ProceedActivityId }
        }
    };
}

/// <summary>
/// The <see cref="DispatchAndWaitInForkWorkflow"/> fork, preceded by a fire-and-forget dispatch of
/// <see cref="FireAndForgetChildWorkflow"/> from the same parent.
/// </summary>
public class DispatchAndWaitBesideFireAndForgetWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.Root = new Sequence
        {
            Activities =
            {
                new DispatchWorkflow { WorkflowDefinitionId = new(nameof(FireAndForgetChildWorkflow)) },
                DispatchAndWaitInForkWorkflow.CreateFork(nameof(BlockingChildWorkflow))
            }
        };
    }
}

/// <summary>The <see cref="DispatchAndWaitInForkWorkflow"/> fork, waiting for a <see cref="FinishingChildWorkflow"/>.</summary>
public class DispatchAndWaitForFinishingChildWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder) => builder.Root = DispatchAndWaitInForkWorkflow.CreateFork(nameof(FinishingChildWorkflow));
}
