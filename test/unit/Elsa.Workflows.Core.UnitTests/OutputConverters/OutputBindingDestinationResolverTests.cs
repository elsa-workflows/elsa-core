using Elsa.Expressions.Models;
using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using System.Threading.Tasks;

namespace Elsa.Workflows.Core.UnitTests.OutputConverters;

public class OutputBindingDestinationResolverTests
{
    private readonly OutputBindingDestinationResolver _resolver = new();

    [Test]
    public async Task Resolve_RuntimeVariable_ReturnsDeclaredTypeAndKind()
    {
        var context = await new ActivityTestFixture(new WriteLine("test")).BuildAsync();
        var variable = new Variable<string>("Destination", "unchanged");
        context.ExpressionExecutionContext.Memory.Declare(variable);
        var output = new Output<int>(variable);

        var destination = _resolver.Resolve(context, output);

        await Assert.That(destination).IsNotNull();
        await Assert.That(destination.Id).IsEqualTo(variable.Id);
        await Assert.That(destination.Type).IsEqualTo(typeof(string));
        await Assert.That(destination.AllowsNull).IsTrue();
        await Assert.That(destination.Kind).IsEqualTo(OutputBindingDestinationKind.Variable);
    }

    [Test]
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

        await Assert.That(destination).IsNotNull();
        await Assert.That(destination.Id).IsEqualTo("workflowResult");
        await Assert.That(destination.Type).IsEqualTo(typeof(int));
        await Assert.That(destination.AllowsNull).IsFalse();
        await Assert.That(destination.Kind).IsEqualTo(OutputBindingDestinationKind.WorkflowOutput);
    }

    [Test]
    public async Task Resolve_StaticVariable_UsesNearestVariableContainer()
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

        await Assert.That(destination).IsNotNull();
        await Assert.That(destination.Id).IsEqualTo(referenceId);
        await Assert.That(destination.Type).IsEqualTo(typeof(string));
        await Assert.That(destination.AllowsNull).IsTrue();
        await Assert.That(destination.Kind).IsEqualTo(OutputBindingDestinationKind.Variable);
    }

    [Test]
    public async Task Resolve_StaticVariable_IncludesCurrentVariableContainer()
    {
        var referenceId = "local-destination";
        var activity = new Sequence
        {
            Id = "activity",
            Variables = [new Variable<string>("LocalDestination", "", referenceId)]
        };
        var node = new ActivityNode(activity, "Body");
        var workflow = new Workflow { Root = activity };
        var graph = new WorkflowGraph(workflow, node, [node]);
        var output = new Output<int>(new MemoryBlockReference(referenceId));

        var destination = _resolver.Resolve(graph, node, output);

        await Assert.That(destination).IsNotNull();
        await Assert.That(destination.Id).IsEqualTo(referenceId);
        await Assert.That(destination.Type).IsEqualTo(typeof(string));
        await Assert.That(destination.AllowsNull).IsTrue();
        await Assert.That(destination.Kind).IsEqualTo(OutputBindingDestinationKind.Variable);
    }

    [Test]
    [Arguments(typeof(string), true)]
    [Arguments(typeof(int?), true)]
    [Arguments(typeof(int), false)]
    public async Task Resolve_StaticWorkflowOutput_ReflectsClrNullability(Type type, bool expectedAllowsNull)
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

        await Assert.That(destination).IsNotNull();
        await Assert.That(destination.Type).IsEqualTo(type);
        await Assert.That(destination.AllowsNull).IsEqualTo(expectedAllowsNull);
    }

    [Test]
    public async Task Resolve_WhenReferenceIsNeitherVariableNorWorkflowOutput_ReturnsNull()
    {
        var activity = new WriteLine("test") { Id = "activity" };
        var node = new ActivityNode(activity, "Body");
        var workflow = new Workflow { Root = activity };
        var graph = new WorkflowGraph(workflow, node, [node]);
        var output = new Output<object>(new MemoryBlockReference("missing"));

        var destination = _resolver.Resolve(graph, node, output);

        await Assert.That(destination).IsNull();
    }

    private static void Connect(ActivityNode parent, ActivityNode child)
    {
        parent.AddChild(child);
        child.AddParent(parent);
    }
}