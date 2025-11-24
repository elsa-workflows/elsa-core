using Elsa.Workflows.Activities;
using Elsa.Workflows.Memory;

namespace Elsa.Workflows.IntegrationTests.Activities.SequenceActivity;

/// <summary>
/// Workflow with Sequence that executes all activities.
/// </summary>
public class SimpleSequenceWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder workflow)
    {
        workflow.Root = new Sequence
        {
            Activities =
            {
                new WriteLine("Activity 1"),
                new WriteLine("Activity 2"),
                new WriteLine("Activity 3")
            }
        };
    }
}

/// <summary>
/// Workflow with Sequence containing a Break activity.
/// </summary>
public class SequenceWithBreakWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder workflow)
    {
        workflow.Root = new Sequence
        {
            Activities =
            {
                new WriteLine("Before Break"),
                new Break(),
                new WriteLine("After Break (should not execute)")
            }
        };
    }
}

/// <summary>
/// Workflow with Sequence where Break is conditional.
/// </summary>
public class SequenceWithConditionalBreakWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder workflow)
    {
        var counter = new Variable<int>("Counter", 0);

        workflow.WithVariables(counter);

        workflow.Root = new Sequence
        {
            Activities =
            {
                new WriteLine("Iteration 1"),
                new SetVariable<int> { Variable = counter, Value = new(1) },
                new WriteLine("Iteration 2"),
                new SetVariable<int> { Variable = counter, Value = new(2) },
                new If(context => counter.Get(context) >= 2)
                {
                    Then = new Break()
                },
                new WriteLine("Iteration 3 (should not execute)")
            }
        };
    }
}

/// <summary>
/// Workflow with nested Sequences and Break.
/// </summary>
public class NestedSequencesWithBreakWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder workflow)
    {
        workflow.Root = new Sequence
        {
            Activities =
            {
                new WriteLine("Outer: Start"),
                new Sequence
                {
                    Activities =
                    {
                        new WriteLine("Inner: Activity 1"),
                        new Break(),
                        new WriteLine("Inner: Activity 2 (should not execute)")
                    }
                },
                new WriteLine("Outer: After inner sequence"),
                new WriteLine("Outer: End")
            }
        };
    }
}

/// <summary>
/// Workflow with Sequence containing Complete activity (terminal node).
/// </summary>
public class SequenceWithCompleteWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder workflow)
    {
        workflow.Root = new Sequence
        {
            Activities =
            {
                new WriteLine("Before Complete"),
                new Complete(),
                new WriteLine("After Complete (should not execute)")
            }
        };
    }
}

/// <summary>
/// Workflow with Sequence and variables.
/// </summary>
public class SequenceWithVariablesWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder workflow)
    {
        var counter = new Variable<int>("Counter", 0);
        var name = new Variable<string>("Name", "Initial");

        workflow.Root = new Sequence
        {
            Variables = { counter, name },
            Activities =
            {
                new WriteLine(context => $"Counter: {counter.Get(context)}, Name: {name.Get(context)}"),
                new SetVariable<int> { Variable = counter, Value = new(10) },
                new SetVariable<string> { Variable = name, Value = new("Updated") },
                new WriteLine(context => $"Counter: {counter.Get(context)}, Name: {name.Get(context)}")
            }
        };
    }
}

/// <summary>
/// Workflow with empty Sequence.
/// </summary>
public class EmptySequenceWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder workflow)
    {
        workflow.Root = new Sequence();
    }
}
