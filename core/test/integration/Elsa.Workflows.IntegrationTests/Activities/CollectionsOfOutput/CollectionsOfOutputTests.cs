using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.Models;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Activities.CollectionsOfOutput;

public class CollectionsOfOutputTests
{
    private readonly IWorkflowRunner _workflowRunner;
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IServiceProvider _services;

    public CollectionsOfOutputTests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper)
            .WithCapturingTextWriter(_capturingTextWriter)
            .AddActivitiesFrom<ProduceDynamicOutputs>()
            .Build();
        _workflowRunner = _services.GetRequiredService<IWorkflowRunner>();
    }

    [Fact(DisplayName = "Each entry of a Dictionary<string, Output<T>> output is readable by its key")]
    public async Task DictionaryOutputsAreResolvableByNameTest()
    {
        await _services.PopulateRegistriesAsync();
        await _workflowRunner.RunAsync<ProduceDynamicOutputsWorkflow>();
        var lines = _capturingTextWriter.Lines.ToArray();
        Assert.Equal(new[] { "name: Frank (string)", "isAdmin: False (bool)", "age: 42 (double)" }, lines);
    }

    [Fact(DisplayName = "Each entry of a Dictionary<string, Output<T>> output is recorded in the output register under its key")]
    public async Task DictionaryOutputsAreResolvableViaRegisterTest()
    {
        await _services.PopulateRegistriesAsync();
        await _workflowRunner.RunAsync<ResolveDynamicOutputsByRegisterWorkflow>();
        var lines = _capturingTextWriter.Lines.ToArray();
        Assert.Equal(new[] { "name: Frank (string)", "isAdmin: False (bool)", "age: 42 (double)" }, lines);
    }

    [Fact(DisplayName = "Each entry of a dictionary output gets its own memory block")]
    public async Task EntryMemoryBlockIdsTest()
    {
        await _services.PopulateRegistriesAsync();
        var producer = new ProduceDynamicOutputs(new Dictionary<string, Output<object>> { ["name"] = new() }) { Id = "Producer1" };

        await _workflowRunner.RunAsync(producer);

        Assert.Equal("Producer1:output-outputs:name", producer.Outputs!["name"].MemoryBlockReference().Id);
    }

    [Fact(DisplayName = "Dictionary output entries keep their memory blocks after a JSON round trip")]
    public async Task JsonRoundTripTest()
    {
        await _services.PopulateRegistriesAsync();
        var serializer = _services.GetRequiredService<IActivitySerializer>();
        var producer = new ProduceDynamicOutputs(new Dictionary<string, Output<object>>
        {
            ["name"] = new(),
            ["age"] = new()
        })
        {
            Id = "Producer1"
        };

        // Unbound outputs are serialized without a memory block ID, so they are read back as null entries.
        var deserialized = (ProduceDynamicOutputs)serializer.Deserialize(serializer.Serialize(producer));
        var result = await _workflowRunner.RunAsync(deserialized);

        Assert.Equal("Producer1:output-outputs:name", deserialized.Outputs!["name"].MemoryBlockReference().Id);
        Assert.Equal("Producer1:output-outputs:age", deserialized.Outputs!["age"].MemoryBlockReference().Id);

        var register = result.WorkflowExecutionContext.GetActivityOutputRegister();
        Assert.Equal("Frank", register.FindOutputByActivityId("Producer1", "name"));
        Assert.Equal(42d, register.FindOutputByActivityId("Producer1", "age"));
    }
}