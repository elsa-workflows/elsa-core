using Elsa.Expressions.Models;
using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.Core.UnitTests.OutputConverters;

public class OutputBindingDestinationResolverTests
{
    private readonly OutputBindingDestinationResolver _resolver = new();

    [Fact]
    public async Task Resolve_RuntimeVariable_ReturnsDeclaredTypeAndKind()
    {
        var context = await new ActivityTestFixture(new WriteLine("test")).BuildAsync();
        var variable = new Variable<string>("Destination", "unchanged");
        context.ExpressionExecutionContext.Memory.Declare(variable);
        var output = new Output<int>(variable);

        var destination = _resolver.Resolve(context, output);

        Assert.NotNull(destination);
        Assert.Equal(variable.Id, destination.Id);
        Assert.Equal(typeof(string), destination.Type);
        Assert.True(destination.AllowsNull);
        Assert.Equal(OutputBindingDestinationKind.Variable, destination.Kind);
    }

    [Fact]
    public async Task Resolve_RuntimeWorkflowOutput_ReturnsWorkflowOutputDefinition()
    {
        var context = await new ActivityTestFixture(new WriteLine("test")).BuildAsync();
        context.WorkflowExecutionContext.Workflow.Outputs.Add(new()
        {
            Name = "workflowResult",
            Type = typeof(int)
        });
        var output = new Output<string>(new MemoryBlockReference("workflowResult"));

        var destination = _resolver.Resolve(context, output);

        Assert.NotNull(destination);
        Assert.Equal("workflowResult", destination.Id);
        Assert.Equal(typeof(int), destination.Type);
        Assert.False(destination.AllowsNull);
        Assert.Equal(OutputBindingDestinationKind.WorkflowOutput, destination.Kind);
    }

    [Fact]
    public void Resolve_StaticVariable_UsesNearestVariableContainer()
    {
        var referenceId = "shared-destination";
        var root = new Sequence
        {
            Id = "root",
            Variables = [new Variable<int>("RootDestination", 0, referenceId)]
        };
        var nearest = new Sequence
        {
            Id = "nearest",
            Variables = [new Variable<string>("NearestDestination", "", referenceId)]
        };
        var activity = new WriteLine("test") { Id = "activity" };
        var rootNode = new ActivityNode(root, "Body");
        var nearestNode = new ActivityNode(nearest, "Body");
        var activityNode = new ActivityNode(activity, "Body");
        Connect(rootNode, nearestNode);
        Connect(nearestNode, activityNode);
        var workflow = new Workflow { Root = root };
        var graph = new WorkflowGraph(workflow, rootNode, [rootNode, nearestNode, activityNode]);
        var output = new Output<int>(new MemoryBlockReference(referenceId));

        var destination = _resolver.Resolve(graph, activityNode, output);

        Assert.NotNull(destination);
        Assert.Equal(referenceId, destination.Id);
        Assert.Equal(typeof(string), destination.Type);
        Assert.True(destination.AllowsNull);
        Assert.Equal(OutputBindingDestinationKind.Variable, destination.Kind);
    }

    [Theory]
    [InlineData(typeof(string), true)]
    [InlineData(typeof(int?), true)]
    [InlineData(typeof(int), false)]
    public void Resolve_StaticWorkflowOutput_ReflectsClrNullability(Type type, bool expectedAllowsNull)
    {
        var activity = new WriteLine("test") { Id = "activity" };
        var node = new ActivityNode(activity, "Body");
        var workflow = new Workflow
        {
            Root = activity,
            Outputs =
            [
                new OutputDefinition
                {
                    Name = "workflowResult",
                    Type = type
                }
            ]
        };
        var graph = new WorkflowGraph(workflow, node, [node]);
        var output = new Output<object>(new MemoryBlockReference("workflowResult"));

        var destination = _resolver.Resolve(graph, node, output);

        Assert.NotNull(destination);
        Assert.Equal(type, destination.Type);
        Assert.Equal(expectedAllowsNull, destination.AllowsNull);
    }

    [Fact]
    public void Resolve_WhenReferenceIsNeitherVariableNorWorkflowOutput_ReturnsNull()
    {
        var activity = new WriteLine("test") { Id = "activity" };
        var node = new ActivityNode(activity, "Body");
        var workflow = new Workflow { Root = activity };
        var graph = new WorkflowGraph(workflow, node, [node]);
        var output = new Output<object>(new MemoryBlockReference("missing"));

        var destination = _resolver.Resolve(graph, node, output);

        Assert.Null(destination);
    }

    private static void Connect(ActivityNode parent, ActivityNode child)
    {
        parent.AddChild(child);
        child.AddParent(parent);
    }
}
