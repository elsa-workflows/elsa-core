using Elsa.Expressions.Models;
using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Activities.CollectionsOfInput;

public class CollectionsOfInputTests
{
    private readonly IWorkflowRunner _workflowRunner;
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IServiceProvider _services;

    public CollectionsOfInputTests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper)
            .WithCapturingTextWriter(_capturingTextWriter)
            .AddActivitiesFrom<WriteMultiLine>()
            .Build();
        _workflowRunner = _services.GetRequiredService<IWorkflowRunner>();
    }

    [Fact(DisplayName = "Each entry of a List<Input<T>> input is evaluated")]
    public async Task CollectionOfInputsTest()
    {
        await _services.PopulateRegistriesAsync();
        await _workflowRunner.RunAsync<WriteMultiLineWorkflow>();
        var lines = _capturingTextWriter.Lines.ToArray();
        Assert.Equal(new[] { "banana", "orange", "apple" }, lines);
    }

    [Fact(DisplayName = "Each value of a Dictionary<string, Input<T>> input is evaluated")]
    public async Task DictionaryOfInputValuesTest()
    {
        await _services.PopulateRegistriesAsync();
        await _workflowRunner.RunAsync<DynamicArgumentsWorkflow>();
        var lines = _capturingTextWriter.Lines.ToArray();
        Assert.Equal(new[] { "name: Frank (string)", "isAdmin: False (bool)", "age: 42 (double)" }, lines);
    }

    [Fact(DisplayName = "Each entry of a collection or dictionary input gets its own memory block")]
    public async Task EntryMemoryBlockIdsTest()
    {
        await _services.PopulateRegistriesAsync();
        var writeMultiLine = new WriteMultiLine([new Input<string>("orange")]) { Id = "WriteMultiLine1" };
        var dynamicArguments = new DynamicArguments(new Dictionary<string, Input<object>> { ["isAdmin"] = new(false) }) { Id = "DynamicArguments1" };

        await _workflowRunner.RunAsync(new Sequence { Activities = { writeMultiLine, dynamicArguments } });

        Assert.Equal("WriteMultiLine1:input-text:1", writeMultiLine.Text![0].MemoryBlockReference().Id);
        Assert.Equal("DynamicArguments1:input-arguments:isAdmin", dynamicArguments.Arguments!["isAdmin"].MemoryBlockReference().Id);
    }

    [Fact(DisplayName = "Collection and dictionary inputs are evaluated after a JSON round trip")]
    public async Task JsonRoundTripTest()
    {
        await _services.PopulateRegistriesAsync();
        var serializer = _services.GetRequiredService<IActivitySerializer>();
        var sequence = new Sequence
        {
            Activities =
            {
                new WriteMultiLine([new Input<string>("orange"), new Input<string>(new Expression("JavaScript", "'apple';"))]),
                new DynamicArguments(new Dictionary<string, Input<object>>
                {
                    ["isAdmin"] = new(false),
                    ["age"] = new(new Expression("JavaScript", "30 + 12"))
                })
            }
        };

        var deserialized = serializer.Deserialize(serializer.Serialize(sequence));
        await _workflowRunner.RunAsync(deserialized);

        var lines = _capturingTextWriter.Lines.ToArray();
        Assert.Equal(new[] { "orange", "apple", "isAdmin: False (bool)", "age: 42 (double)" }, lines);
    }
}