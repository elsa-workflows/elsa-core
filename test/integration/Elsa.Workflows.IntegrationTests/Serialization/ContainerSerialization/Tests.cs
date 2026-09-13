using Elsa.Expressions.Models;
using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Activities.Flowchart.Activities;
using Elsa.Workflows.Activities.Flowchart.Models;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.IntegrationTests.Serialization.ContainerSerialization;

public class Tests : IAsyncDisposable
{
    private readonly IServiceProvider _services;
    private readonly IActivitySerializer _activitySerializer;

    public Tests()
    {
        _services = new TestApplicationBuilder(TestContext.Current!.Output.StandardOutput).Build();
        _activitySerializer = _services.GetService<IActivitySerializer>()!;
    }

    [Test]
    public async Task SerializeFlowchartContainerTest()
    {
        await _services.PopulateRegistriesAsync();

        // Arrange

        var start = new Start
        {
            Id = "start",
            Name = "Start",
            RunAsynchronously = false // Manually set to false because the manual construction defaults to null,
                                      // But deserialization uses the factory creation method that overwrites null values.
        };
        var writeLine = new WriteLine(new Input<string>(new Expression("JavaScript", "getVariable('TextVar')")))
        {
            Id = "writeLine",
            Name = "WriteLine",
            Version = 3,
            RunAsynchronously = false // Manually set to false because the manual construction defaults to null,
                                      // But deserialization uses the factory creation method that overwrites null values.
        };
        var end = new End
        {
            Id = "end",
            Name = "end",
            RunAsynchronously = false // Manually set to false because the manual construction defaults to null,
                                      // But deserialization uses the factory creation method that overwrites null values.
        };
        var container = new Flowchart
        {
            Id = "flowchart",
            Name = "Flowchart",
            Type = "Elsa.Flowchart",
            Version = 42,
            CustomProperties = new Dictionary<string, object>
            {
                { "purpose", "somePurpose" }
            },
            Metadata = new Dictionary<string, object>
            {
                { "int", 10 },
                { "bool", false },
                { "string", "str" },
            },
            Activities = new List<IActivity> {
                start,
                writeLine,
                end
            },
            Variables = new List<Variable> {
                new Variable<string>("TextVar", "This is the text to write")
            },
            Connections = new List<Connection>
            {
                new(start, writeLine),
                new(writeLine, end),
            },
            RunAsynchronously = false
        };

        // Act

        var serialized = _activitySerializer.Serialize(container);
        var deserializedContainer = _activitySerializer.Deserialize(serialized) as Container;

        // Assert

        await ValidateContainerAsync(container, deserializedContainer);
    }

    [Test]
    public async Task SerializeSequenceContainerTest()
    {
        await _services.PopulateRegistriesAsync();

        // Arrange
        var container = new Sequence
        {
            Id = "sequence",
            Name = "Sequence",
            Type = "Elsa.Sequence",
            Version = 42,
            Variables = new List<Variable> {
                new Variable<string>("TextVar", "This is the text to write")
            },
            Activities = new List<IActivity> {
                new WriteLine(new Input<string>(new Expression("JavaScript", "getVariable('TextVar')")))
                {
                    Id = "writeLine",
                    Name = "WriteLine",
                    CanStartWorkflow = true,
                    RunAsynchronously = false // Manually set to false because the manual construction defaults to null,
                                              // But deserialization uses the factory creation method that overwrites null values.
                },
            },
            CustomProperties = new Dictionary<string, object>
            {
                {  "purpose", "somePurpose" }
            },
            Metadata = new Dictionary<string, object>
            {
                { "int", 10 },
                { "bool", false },
                { "string", "str"},
            },
            RunAsynchronously = false
        };

        // Act

        var serialized = _activitySerializer.Serialize(container);
        var deserializedContainer = _activitySerializer.Deserialize(serialized) as Container;

        // Assert

        await ValidateContainerAsync(container, deserializedContainer);
    }

    [Test]
    public async Task SerializeParallelContainerTest()
    {
        await _services.PopulateRegistriesAsync();

        // Arrange
        var container = new Workflows.Activities.Parallel
        {
            Id = "parallel",
            Name = "Parallel",
            Type = "Elsa.Parallel",
            Version = 42,
            Variables = new List<Variable> {
                new Variable<string>("TextVar", "This is the text to write")
            },
            Activities = new List<IActivity> {
                new WriteLine(new Input<string>(new Expression("JavaScript", "getVariable('TextVar')")))
                {
                    Id = "writeLine",
                    Name = "WriteLine",
                    CanStartWorkflow = true,
                    RunAsynchronously = false // Manually set to false because the manual construction defaults to null,
                                              // But deserialization uses the factory creation method that overwrites null values.
                    
                },
            },
            CustomProperties = new Dictionary<string, object>
            {
                {  "purpose", "somePurpose" }
            },
            Metadata = new Dictionary<string, object>
            {
                { "int", 10 },
                { "bool", false },
                { "string", "str"},
            },
            RunAsynchronously = false
        };

        // Act

        var serialized = _activitySerializer.Serialize(container);
        var deserializedContainer = _activitySerializer.Deserialize(serialized) as Container;

        // Assert

        await ValidateContainerAsync(container, deserializedContainer);
    }

    private static async Task ValidateContainerAsync(Container container, Container? deserializedContainer)
    {
        var actual = await Assert.That(deserializedContainer).IsNotNull();

        // Structural equivalency has trouble with the Behavior.Owner reference - since these aren't serialized anyway, ignore them.
        actual.Behaviors.Clear();
        container.Behaviors.Clear();
        foreach (var activity1 in actual.Activities)
        {
            var activity = (Activity)activity1;
            activity.Behaviors.Clear();
        }

        foreach (var activity in container.Activities.Cast<Activity>())
        {
            activity.Behaviors.Clear();
        }

        await Assert.That(actual).IsEquivalentTo(container, strict: false);
    }

    public ValueTask DisposeAsync() => TestResourceDisposal.DisposeAsync(_services);
}
