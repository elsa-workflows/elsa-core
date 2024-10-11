using Elsa.Expressions.Models;
using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Activities.Flowchart.Activities;
using Elsa.Workflows.Activities.Flowchart.Models;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Serialization.ContainerSerialization;

public class Tests
{
    private readonly IServiceProvider _services;
    private readonly IActivitySerializer _activitySerializer;

    public Tests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper).Build();
        _activitySerializer = _services.GetService<IActivitySerializer>()!;
    }

    [Fact]
    public async void SerializeFlowchartContainerTest()
    {
        await _services.PopulateRegistriesAsync();

        // Arrange

        var start = new Start()
        {
            Id = "start",
            Name = "Start",
        };
        var writeLine = new WriteLine(new Input<string>(new Expression("JavaScript", "getVariable('TextVar')")))
        {
            Id = "writeLine",
            Name = "WriteLine",
            Version = 3,
        };
        var end = new End()
        {
            Id = "end",
            Name = "end",
        };
        var container = new Flowchart()
        {
            Id = "flowchart",
            Name = "Flowchart",
            Type = "Elsa.Flowchart",
            Version = 42,
            CustomProperties = new Dictionary<string, object>()
            {
                { "purpose", "somePurpose" }
            },
            Metadata = new Dictionary<string, object>()
            {
                { "int", 10 },
                { "bool", false },
                { "string", "str" },
            },
            Activities = new List<IActivity>() {
                start,
                writeLine,
                end
            },
            Variables = new List<Variable>() {
                new Variable<string>("TextVar", "This is the text to write")
            },
            Connections = new List<Connection>()
            {
                new Connection(start, writeLine),
                new Connection(writeLine, end),
            },
        };

        // Act

        var serialized = _activitySerializer.Serialize(container);
        var deserializedContainer = _activitySerializer.Deserialize(serialized) as Container;

        // Assert

        ValidateContainer(container, deserializedContainer);
    }

    [Fact]
    public async void SerializeSequenceContainerTest()
    {
        await _services.PopulateRegistriesAsync();

        // Arrange

        var container = new Sequence()
        {
            Id = "sequence",
            Name = "Sequence",
            Type = "Elsa.Sequence",
            Version = 42,
            Variables = new List<Variable>() {
                new Variable<string>("TextVar", "This is the text to write")
            },
            Activities = new List<IActivity>() {
                new WriteLine(new Input<string>(new Expression("JavaScript", "getVariable('TextVar')")))
                {
                    Id = "writeLine",
                    Name = "WriteLine",
                    CanStartWorkflow = true,
                },
            },
            CustomProperties = new Dictionary<string, object>()
            {
                {  "purpose", "somePurpose" }
            },
            Metadata = new Dictionary<string, object>()
            {
                { "int", 10 },
                { "bool", false },
                { "string", "str"},
            }
        };

        // Act

        var serialized = _activitySerializer.Serialize(container);
        var deserializedContainer = _activitySerializer.Deserialize(serialized) as Container;

        // Assert

        ValidateContainer(container, deserializedContainer);
    }

    [Fact]
    public async void SerializeParallelContainerTest()
    {
        await _services.PopulateRegistriesAsync();

        // Arrange

        var container = new Workflows.Activities.Parallel()
        {
            Id = "parallel",
            Name = "Parallel",
            Type = "Elsa.Parallel",
            Version = 42,
            Variables = new List<Variable>() {
                new Variable<string>("TextVar", "This is the text to write")
            },
            Activities = new List<IActivity>() {
                new WriteLine(new Input<string>(new Expression("JavaScript", "getVariable('TextVar')")))
                {
                    Id = "writeLine",
                    Name = "WriteLine",
                    CanStartWorkflow = true,
                },
            },
            CustomProperties = new Dictionary<string, object>()
            {
                {  "purpose", "somePurpose" }
            },
            Metadata = new Dictionary<string, object>()
            {
                { "int", 10 },
                { "bool", false },
                { "string", "str"},
            }
        };

        // Act

        var serialized = _activitySerializer.Serialize(container);
        var deserializedContainer = _activitySerializer.Deserialize(serialized) as Container;

        // Assert

        ValidateContainer(container, deserializedContainer);
    }

    private static void ValidateContainer(Container container, Container? deserializedContainer)
    {
        if (deserializedContainer == null)
            throw new ArgumentNullException(nameof(deserializedContainer));

        // Assert.Equivalent has trouble with the Behavior.Owner reference - since these aren't serialzied anyway, ignore them
        deserializedContainer.Behaviors.Clear();
        container.Behaviors.Clear();
        foreach (Activity activity in deserializedContainer.Activities)
            activity.Behaviors.Clear();
        foreach (Activity activity in container.Activities)
            activity.Behaviors.Clear();

        // strict:false here allows "actual" to have extra public members that aren't part of "expected", and collection
        // comparison allows "actual" to have more data in it than is present in "expected".
        Assert.Equivalent(container, deserializedContainer, strict: false);
    }
}
