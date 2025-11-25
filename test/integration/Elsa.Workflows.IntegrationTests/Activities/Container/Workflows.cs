using Elsa.Testing.Shared.Activities;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Memory;

namespace Elsa.Workflows.IntegrationTests.Activities.Container;

/// <summary>
/// Simple workflow with a container that executes activities sequentially.
/// </summary>
public class SimpleContainerWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder workflow)
    {
        workflow.Root = new TestContainer
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
/// Workflow demonstrating container with variables.
/// </summary>
public class ContainerWithVariablesWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder workflow)
    {
        var counter = new Variable<int>("Counter", 0);

        workflow.Root = new TestContainer
        {
            Variables = { counter },
            Activities =
            {
                new WriteLine(context => $"Counter: {counter.Get(context)}"),
                new SetVariable<int> { Variable = counter, Value = new(1) },
                new WriteLine(context => $"Counter: {counter.Get(context)}"),
                new SetVariable<int> { Variable = counter, Value = new(2) },
                new WriteLine(context => $"Counter: {counter.Get(context)}")
            }
        };
    }
}

/// <summary>
/// Workflow with nested containers to test variable scoping.
/// </summary>
public class NestedContainersWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder workflow)
    {
        var outerVar = new Variable<int>("OuterVar", 10);
        var innerVar = new Variable<int>("InnerVar", 20);

        workflow.Root = new TestContainer
        {
            Variables = { outerVar },
            Activities =
            {
                new WriteLine(context => $"Outer: {outerVar.Get(context)}"),
                new TestContainer
                {
                    Variables = { innerVar },
                    Activities =
                    {
                        new WriteLine(context => $"Inner: {innerVar.Get(context)}")
                    }
                },
                new WriteLine(context => $"Outer again: {outerVar.Get(context)}")
            }
        };
    }
}

/// <summary>
/// Workflow with unnamed variables to test auto-naming.
/// </summary>
public class ContainerWithUnnamedVariablesWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder workflow)
    {
        workflow.Root = new TestContainer
        {
            Variables =
            {
                new Variable<int>(),
                new Variable<string>(),
                new Variable<bool>()
            },
            Activities =
            {
                new WriteLine("Unnamed variables workflow")
            }
        };
    }
}

/// <summary>
/// Workflow with an empty container (no activities).
/// </summary>
public class EmptyContainerWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder workflow)
    {
        workflow.Root = new TestContainer();
    }
}

/// <summary>
/// Workflow with mixed variable types.
/// </summary>
public class ContainerWithMixedVariableTypesWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder workflow)
    {
        var intVar = new Variable<int>("IntVar", 42);
        var stringVar = new Variable<string>("StringVar", "Hello");
        var boolVar = new Variable<bool>("BoolVar", true);

        workflow.Root = new TestContainer
        {
            Variables = { intVar, stringVar, boolVar },
            Activities =
            {
                new WriteLine(context => $"Int: {intVar.Get(context)}"),
                new WriteLine(context => $"String: {stringVar.Get(context)}"),
                new WriteLine(context => $"Bool: {boolVar.Get(context)}")
            }
        };
    }
}

/// <summary>
/// Workflow with a dynamic number of activities.
/// </summary>
public class DynamicContainerWorkflow(int activityCount) : WorkflowBase
{
    protected override void Build(IWorkflowBuilder workflow)
    {
        var container = new TestContainer();

        for (var i = 1; i <= activityCount; i++)
        {
            container.Activities.Add(new WriteLine($"Activity {i}"));
        }

        workflow.Root = container;
    }
}
