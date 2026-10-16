using Elsa.Workflows.Activities;
using Elsa.Workflows.IntegrationTests.Scenarios.DispatchWorkflowCancellation;
using Elsa.Workflows.Runtime.Activities;

namespace Elsa.Workflows.IntegrationTests.Scenarios.BulkDispatchWorkflowsCancellation;

/// <summary>The blocking child under its own definition, dispatched by a second bulk activity so a test can tell it apart.</summary>
public class SecondBulkChildWorkflow : BlockingChildWorkflow;

/// <summary>
/// A parent that bulk-dispatches <see cref="BlockingChildWorkflow"/> instances and waits for them on one fork branch, and
/// blocks on a second branch the test resumes. The fork waits for any branch, so resuming the second one completes the
/// fork and cancels the still-waiting <see cref="BulkDispatchWorkflows"/> activity. There are more children than the
/// activity reads in one page (100) when it looks for them, so cancelling them takes more than one page.
/// </summary>
public class BulkDispatchAndWaitInForkWorkflow : WorkflowBase
{
    public const string ProceedActivityId = "proceed";
    public const int ChildCount = 101;

    protected override void Build(IWorkflowBuilder builder) => builder.Root = CreateFork(ChildCount);

    /// <summary>The fork that waits for the bulk-dispatched children on one branch and for the test on the other.</summary>
    public static Fork CreateFork(int childCount) => CreateFork(nameof(BlockingChildWorkflow), childCount);

    /// <summary>The fork that waits for the specified child definition on one branch and for the test on the other.</summary>
    public static Fork CreateFork(string childDefinitionId, int childCount) => new()
    {
        JoinMode = ForkJoinMode.WaitAny,
        Branches =
        {
            new BulkDispatchWorkflows
            {
                WorkflowDefinitionId = new(childDefinitionId),
                Items = new(Enumerable.Range(0, childCount).Cast<object>().ToArray())
            },
            new Event("ProceedParent") { Id = ProceedActivityId }
        }
    };
}

/// <summary>
/// The <see cref="BulkDispatchAndWaitInForkWorkflow"/> fork, waiting for a single <see cref="FinishingChildWorkflow"/>.
/// </summary>
public class BulkDispatchAndWaitForFinishingChildWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder) => builder.Root = BulkDispatchAndWaitInForkWorkflow.CreateFork(nameof(FinishingChildWorkflow), 1);
}

/// <summary>
/// A parent that dispatches a <see cref="FireAndForgetChildWorkflow"/>, then runs the
/// <see cref="BulkDispatchAndWaitInForkWorkflow"/> fork beside a second waiting <see cref="BulkDispatchWorkflows"/> of
/// <see cref="SecondBulkChildWorkflow"/>. The outer fork waits for all branches, so cancelling the first bulk activity
/// leaves the second one waiting.
/// </summary>
public class BulkDispatchAndWaitBesideSiblingDispatchesWorkflow : WorkflowBase
{
    public const int WaitedChildCount = 2;

    protected override void Build(IWorkflowBuilder builder)
    {
        builder.Root = new Sequence
        {
            Activities =
            {
                new DispatchWorkflow { WorkflowDefinitionId = new(nameof(FireAndForgetChildWorkflow)) },
                new Fork
                {
                    JoinMode = ForkJoinMode.WaitAll,
                    Branches =
                    {
                        BulkDispatchAndWaitInForkWorkflow.CreateFork(WaitedChildCount),
                        new BulkDispatchWorkflows
                        {
                            WorkflowDefinitionId = new(nameof(SecondBulkChildWorkflow)),
                            Items = new(new object[] { 0 })
                        }
                    }
                }
            }
        };
    }
}
